using LoanSplitter.Domain;

namespace LoanSplitter.Events;

public class InterestRateChangedEvent(DateTime date, string loanName, double rate)
    : EventBase(date)
{
    public string LoanName { get; } = loanName;
    public double Rate { get; } = rate;

    public override EventOutcome Apply(State state)
    {
        // Fetch the loan from the state.
        // Loan.ApplyInterestRateChange(rate);
        //    Store pending interest rate change. 
        //    (since we don't want to estimate how much the payment will be.
        var loan = state.GetEntityByName<Loan>(LoanName);
        var newLoan = loan.WithInterestRate(Rate);

        return new EventOutcome(new Dictionary<string, object>
        {
            { LoanName, newLoan }
        });
    }
}