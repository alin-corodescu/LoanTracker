using System.Data;

namespace LoanSplitter.Domain;

public class Loan
{
    public record LoanPayment(double Principal, double Interest, double Fee)
    {
        public Dictionary<string,LoanPayment> Divide(Dictionary<string,double> loanShares)
        {
            return loanShares.ToDictionary(kvp => kvp.Key,
                kvp => new LoanPayment(
                    Principal * kvp.Value, // principal is divided by share. (share is [0,1] so multiply)
                    Interest * kvp.Value, // interest is divided by share. (share is [0,1] so multiply)
                    Fee / loanShares.Count)); // fee is split equally.
        }
    }

    private double _amount;
    private double _annualInterestRate;
    public int RemainingTermInMonths { get; private set; }

    private double? _upcomingInterestRate;
    private Dictionary<string, Loan> _subLoans = new();
    private LoanPayment? _monthlyPaymentOverride;
    private List<AccountTransaction> _advancePayments = new();
    
    // todo add a way to change the monthly fee via an event.
    private double _monthlyFee = 65;

    private Loan Clone()
    {
        var newLoan = new Loan(_amount, _annualInterestRate, RemainingTermInMonths);
        newLoan._subLoans = this._subLoans
            .ToDictionary(
                k => k.Key, 
                v => v.Value.Clone());
        
        newLoan._upcomingInterestRate = this._upcomingInterestRate;
        newLoan._monthlyPaymentOverride = this._monthlyPaymentOverride;
        newLoan._advancePayments = new List<AccountTransaction>(this._advancePayments);
        
        return newLoan;
    }
    
    
    public Loan(double amount, double annualInterestRate, int remainingTermInMonths, params string[] names)
    {
        _amount = amount;
        _annualInterestRate = annualInterestRate;
        RemainingTermInMonths = remainingTermInMonths;

        if (names is { Length: > 0 })
        {
            // assuming that a loan has 2 participants if it is split 
            _subLoans = new Dictionary<string, Loan>
            {
                { names[0], new Loan(amount / 2, annualInterestRate, remainingTermInMonths) },
                { names[1], new Loan(amount - amount / 2, annualInterestRate, remainingTermInMonths) }
            };
        }
    }
    
    public Loan WithInterestRate(double rate)
    {
        var updatedLoan = this.Clone();
        
        updatedLoan._upcomingInterestRate = rate;
        
        // the sub loans have already been cloned.
        foreach (var subLoansValue in updatedLoan._subLoans.Values)
        {
            subLoansValue._upcomingInterestRate = rate;
        }
        
        return updatedLoan;
    }

    public Loan WithExecuteNextPayment()
    {
        // Calculate the payment sum
        var updatedLoan = this.Clone();

        var subLoanSplit = this.GetNextMonthlySplitPayment();
        
        // reduce the amount from the main loan, also reduce by the principal payment in all sub-loans.
        foreach (var subLoanValue in subLoanSplit)
        {
            // Reduce the remaining principal.
            updatedLoan._amount -= subLoanValue.Value.Principal;
            updatedLoan._subLoans[subLoanValue.Key]._amount -= subLoanValue.Value.Principal;
            
            // reduce the remaining term in months
            updatedLoan._subLoans[subLoanValue.Key].RemainingTermInMonths--;
        }
        
        updatedLoan.RemainingTermInMonths--;
        updatedLoan.ApplyAdvancePayments();
        updatedLoan.ApplyPendingInterestRateChange();
        updatedLoan._monthlyPaymentOverride = null;
        return updatedLoan;
    }

    public Dictionary<string, LoanPayment> GetNextMonthlySplitPayment()
    {
        var totalPayment = this.GetNextMonthlyPayment();
        
        var loanShares = this.GetSubLoanShares();

        Dictionary<string, LoanPayment> dividedPayments = totalPayment.Divide(loanShares);
        
        return dividedPayments;

        // todo add event to correct the loan remaining amount. 
        // todo add a rule So we need to add the correct payment after adv payment. if we want to reflect the real payment amount.
        // todo add a validation that the sub loans always sum up to the loan principal.
    }

    private Dictionary<string, double> GetSubLoanShares()
    {
        // this._amount, in theory, should sum up to subLoans._amount.
        return _subLoans.ToDictionary(
            sl => sl.Key,
            sl => sl.Value._amount / this._amount);
    }

    public LoanPayment GetNextMonthlyPayment()
    {
        if (this._monthlyPaymentOverride != null)
            return this._monthlyPaymentOverride;
        
        var monthlyRate = _annualInterestRate / 12 / 100;
        var monthlyPayment = _amount * monthlyRate / (1 - Math.Pow(1 + monthlyRate, -RemainingTermInMonths));
        
        var thisMonthInterest = this._amount * monthlyRate;
        return new LoanPayment(monthlyPayment - thisMonthInterest, thisMonthInterest, this._monthlyFee);
    }

    public Loan WithCorrectNextPayment(double principal, double interest)
    {
        var updatedLoan = this.Clone();

        // We apply all the pending changes before applying the correction.
        // These will mutate the loan in-line
        updatedLoan.ApplyAdvancePayments();
        updatedLoan.ApplyPendingInterestRateChange();
        
        updatedLoan._monthlyPaymentOverride = new(principal, interest, this._monthlyFee);
        
        return updatedLoan;
    }

    private void ApplyPendingInterestRateChange()
    {
        // Modfify the interest rate.
        if (this._upcomingInterestRate.HasValue)
        {
            this._annualInterestRate = this._upcomingInterestRate.Value;
            this._upcomingInterestRate = null;
        }
    }

    private void ApplyAdvancePayments()
    {
        foreach (var advPayment in this._advancePayments)
        {
            // Reduce the amount from the main loan.
            this._amount -= advPayment.Amount;
            
            // Reduce the amount from the sub loan of the person who made the payment.
            this._subLoans[advPayment.PersonName]._amount -= advPayment.Amount;
        }
        
        this._advancePayments.Clear();
    }

    public Loan WithAdvancePayment(AccountTransaction transaction)
    {
        // Store the Advance payment until the Execute next Payment.
        var updatedLoan = this.Clone();
        
        updatedLoan._advancePayments.Add(transaction);
        
        return updatedLoan;
    }
}