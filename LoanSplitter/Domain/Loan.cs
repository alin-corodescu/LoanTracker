namespace LoanSplitter.Domain;

public class Loan
{
    private List<AccountTransaction> _advancePayments = new();

    private double _annualInterestRate;

    // todo add a way to change the monthly fee via an event.
    private readonly double _monthlyFee = 65;
    private LoanPayment? _monthlyPaymentOverride;

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