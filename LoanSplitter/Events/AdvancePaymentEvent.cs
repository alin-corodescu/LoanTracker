using LoanSplitter.Domain;

namespace LoanSplitter.Events;

public class AdvancePaymentEvent(DateTime date, string loanName, AccountTransaction transaction) : EventBase(date)
{
    public string LoanName { get; } = loanName;
    public AccountTransaction Transaction { get; } = transaction;

    public override EventOutcome Apply(State state)
    {
        var loan = state.GetEntityByName<Loan>(LoanName);

        var updatedLoan = loan.WithAdvancePayment(Transaction);

        return new EventOutcome(new Dictionary<string, object> { { LoanName, updatedLoan } });
    }
}