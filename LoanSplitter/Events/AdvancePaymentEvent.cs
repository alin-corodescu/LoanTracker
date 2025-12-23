using LoanSplitter.Domain;

namespace LoanSplitter.Events;

public class AdvancePaymentEvent(DateTime date, string loanName, AccountTransaction transaction) : EventBase(date)
{
    public override EventOutcome Apply(State state)
    {
        var loan = state.GetEntityByName<Loan>(loanName);

        var updatedLoan = loan.WithAdvancePayment(transaction);

        return new EventOutcome(new Dictionary<string, object> { { loanName, updatedLoan } });
    }
}