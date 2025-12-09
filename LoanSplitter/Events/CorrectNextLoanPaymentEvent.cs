using LoanSplitter.Domain;

namespace LoanSplitter.Events;

public class CorrectNextLoanPaymentEvent(DateTime date, string loanName, double principal, double interest) : EventBase(date)
{
    public override EventOutcome Apply(State state)
    {
        var loan = state.GetEntityByName<Loan>(loanName);

        var updatedLoan = loan.WithCorrectNextPayment(principal, interest);
        
        return new EventOutcome(new() { { loanName, updatedLoan } });
    }
}