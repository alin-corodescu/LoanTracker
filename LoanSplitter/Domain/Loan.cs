namespace LoanSplitter.Domain;

public class Loan
{
    private List<AccountTransaction> _advancePayments = new();

    private double _annualInterestRate;

    // todo add a way to change the monthly fee via an event.
    private readonly double _monthlyFee = 65;
    private LoanPayment? _monthlyPaymentOverride;
    private Dictionary<string, double>? _nextPaymentShareOverride;

    private double? _upcomingInterestRate;
    
    public Loan(double remainingAmount, double annualInterestRate, int remainingTermInMonths, params string[] names)
    {
        RemainingAmount = remainingAmount;
        _annualInterestRate = annualInterestRate;
        RemainingTermInMonths = remainingTermInMonths;

        if (names is { Length: > 0 })
            // assuming that a loan has 2 participants if it is split 
            SubLoans = new Dictionary<string, Loan>
            {
                { names[0], new Loan(remainingAmount / 2, annualInterestRate, remainingTermInMonths) },
                { names[1], new Loan(remainingAmount - remainingAmount / 2, annualInterestRate, remainingTermInMonths) }
            };
    }

    public double RemainingAmount { get; private set; }

    public int RemainingTermInMonths { get; private set; }
    
    public Dictionary<string, Loan> SubLoans { get; private set; } = new();

    private Loan Clone()
    {
        var newLoan = new Loan(RemainingAmount, _annualInterestRate, RemainingTermInMonths);
        newLoan.SubLoans = SubLoans
            .ToDictionary(
                k => k.Key,
                v => v.Value.Clone());

        newLoan._upcomingInterestRate = _upcomingInterestRate;
        newLoan._monthlyPaymentOverride = _monthlyPaymentOverride;
        newLoan._advancePayments = new List<AccountTransaction>(_advancePayments);
        newLoan._nextPaymentShareOverride = _nextPaymentShareOverride?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return newLoan;
    }

    public Loan WithInterestRate(double rate)
    {
        var updatedLoan = Clone();

        updatedLoan._upcomingInterestRate = rate;

        // the sub loans have already been cloned.
        foreach (var subLoansValue in updatedLoan.SubLoans.Values) subLoansValue._upcomingInterestRate = rate;

        return updatedLoan;
    }

    public Loan WithExecuteNextPayment()
    {
        // Calculate the payment sum
        var updatedLoan = Clone();

        var subLoanSplit = GetNextMonthlySplitPayment();

        // reduce the amount from the main loan, also reduce by the principal payment in all sub-loans.
        foreach (var subLoanValue in subLoanSplit)
        {
            // Reduce the remaining principal.
            updatedLoan.RemainingAmount -= subLoanValue.Value.Principal;
            updatedLoan.TotalInterestPaid += subLoanValue.Value.Interest;
            updatedLoan.TotalFeesPaid += subLoanValue.Value.Fee;
            
            updatedLoan.SubLoans[subLoanValue.Key].RemainingAmount -= subLoanValue.Value.Principal;
            updatedLoan.SubLoans[subLoanValue.Key].TotalInterestPaid += subLoanValue.Value.Interest;
            updatedLoan.SubLoans[subLoanValue.Key].TotalFeesPaid += subLoanValue.Value.Fee;
            
            // reduce the remaining term in months
            updatedLoan.SubLoans[subLoanValue.Key].RemainingTermInMonths--;
        }

        updatedLoan.RemainingTermInMonths--;
        updatedLoan.ApplyAdvancePayments();
        updatedLoan.ApplyPendingInterestRateChange();
        updatedLoan._monthlyPaymentOverride = null;
        updatedLoan._nextPaymentShareOverride = null;
        return updatedLoan;
    }

    public double TotalFeesPaid { get; private set; }

    public double TotalInterestPaid { get; private set; }

    public Dictionary<string, LoanPayment> GetNextMonthlySplitPayment()
    {
        var totalPayment = GetNextMonthlyPayment();

        var loanShares = GetSubLoanShares();

        var dividedPayments = totalPayment.Divide(loanShares);

        return dividedPayments;

        // todo add event to correct the loan remaining amount. 
        // todo add a rule So we need to add the correct payment after adv payment. if we want to reflect the real payment amount.
        // todo add a validation that the sub loans always sum up to the loan principal.
    }

    private Dictionary<string, double> GetSubLoanShares()
    {
        if (_nextPaymentShareOverride is { Count: > 0 })
            return _nextPaymentShareOverride.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // this._amount, in theory, should sum up to subLoans._amount.
        return SubLoans.ToDictionary(
            sl => sl.Key,
            sl => sl.Value.RemainingAmount / RemainingAmount);
    }

    public LoanPayment GetNextMonthlyPayment()
    {
        if (_monthlyPaymentOverride != null)
            return _monthlyPaymentOverride;

        var monthlyRate = _annualInterestRate / 12 / 100;
        var monthlyPayment = RemainingAmount * monthlyRate / (1 - Math.Pow(1 + monthlyRate, -RemainingTermInMonths));

        var thisMonthInterest = RemainingAmount * monthlyRate;
        return new LoanPayment(monthlyPayment - thisMonthInterest, thisMonthInterest, _monthlyFee);
    }

    public double GetProjectedInterestRemaining()
    {
        if (RemainingTermInMonths <= 0 || RemainingAmount <= 0)
            return 0;

        var monthlyRate = _annualInterestRate / 12 / 100;

        if (monthlyRate <= 0)
            return 0;

        var denominator = 1 - Math.Pow(1 + monthlyRate, -RemainingTermInMonths);
        if (Math.Abs(denominator) < 1e-12)
            return 0;

        var monthlyPayment = RemainingAmount * monthlyRate / denominator;
        var projectedInterest = monthlyPayment * RemainingTermInMonths - RemainingAmount;

        return Math.Max(0, projectedInterest);
    }

    public Loan WithCorrectNextPayment(double principal, double interest)
    {
        var updatedLoan = Clone();

        // We apply all the pending changes before applying the correction.
        // These will mutate the loan in-line
        updatedLoan.ApplyAdvancePayments();
        updatedLoan.ApplyPendingInterestRateChange();

        updatedLoan._monthlyPaymentOverride = new LoanPayment(principal, interest, _monthlyFee);

        return updatedLoan;
    }

    public Loan WithCorrectNextPaymentSplit(IReadOnlyDictionary<string, double> contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);

        if (!SubLoans.Any())
            throw new InvalidOperationException("Cannot override contributions for a loan without participants.");

        if (contributions.Count == 0)
            throw new ArgumentException("At least one contribution override must be provided.", nameof(contributions));

        var updatedLoan = Clone();
        updatedLoan._nextPaymentShareOverride = updatedLoan.NormalizeContributionOverrides(contributions);

        return updatedLoan;
    }

    private void ApplyPendingInterestRateChange()
    {
        // Modfify the interest rate.
        if (_upcomingInterestRate.HasValue)
        {
            _annualInterestRate = _upcomingInterestRate.Value;
            _upcomingInterestRate = null;
        }
    }

    private void ApplyAdvancePayments()
    {
        foreach (var advPayment in _advancePayments)
        {
            // Reduce the amount from the main loan.
            RemainingAmount -= advPayment.Amount;

            // Reduce the amount from the sub loan of the person who made the payment.
            SubLoans[advPayment.PersonName].RemainingAmount -= advPayment.Amount;
        }

        _advancePayments.Clear();
    }

    public Loan WithAdvancePayment(AccountTransaction transaction)
    {
        // Store the Advance payment until the Execute next Payment.
        var updatedLoan = Clone();

        updatedLoan._advancePayments.Add(transaction);

        return updatedLoan;
    }

    private Dictionary<string, double> NormalizeContributionOverrides(IReadOnlyDictionary<string, double> contributions)
    {
        var allowedParticipants = new HashSet<string>(SubLoans.Keys);

        if (allowedParticipants.Count == 0)
            throw new InvalidOperationException("Loan has no sub-loans to apply contribution overrides to.");

        double total = 0;

        foreach (var kvp in contributions)
        {
            if (!allowedParticipants.Contains(kvp.Key))
                throw new InvalidOperationException($"Unknown participant '{kvp.Key}' in contribution overrides.");

            if (kvp.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(contributions), "Contribution overrides must be non-negative.");

            total += kvp.Value;
        }

        if (total <= 0)
            throw new InvalidOperationException("Contribution overrides must sum to a positive value.");

        var normalized = new Dictionary<string, double>();

        foreach (var participant in allowedParticipants)
        {
            var contribution = contributions.TryGetValue(participant, out var amount) ? amount : 0;
            normalized[participant] = contribution / total;
        }

        return normalized;
    }

    public record LoanPayment(double Principal, double Interest, double Fee)
    {
        public Dictionary<string, LoanPayment> Divide(Dictionary<string, double> loanShares)
        {
            return loanShares.ToDictionary(kvp => kvp.Key,
                kvp => new LoanPayment(
                    Principal * kvp.Value, // principal is divided by share. (share is [0,1] so multiply)
                    Interest * kvp.Value, // interest is divided by share. (share is [0,1] so multiply)
                    Fee / loanShares.Count)); // fee is split equally.
        }
    }
}