using LoanSplitter.Domain;

namespace LoanSplitter.Events;

public class InterestRateChangedEvent(DateTime date, string loanName, double rate)
    : EventBase(date)
{
    public override EventOutcome Apply(State state)
    {
        // Fetch the loan from the state.
        // Loan.ApplyInterestRateChange(rate);
        //    Store pending interest rate change. 
        //    (since we don't want to estimate how much the payment will be.
        var loan = state.GetEntityByName<Loan>(loanName);
        var newLoan = loan.WithInterestRate(rate);

        return new EventOutcome(new Dictionary<string, object>()
        {
            { loanName, newLoan }
        });
    }
}