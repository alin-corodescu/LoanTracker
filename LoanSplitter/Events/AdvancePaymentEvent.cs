using LoanSplitter.Domain;

namespace LoanSplitter.Events;

/// <summary>
/// Someone makes an extra principal payment outside the standard schedule.
/// Temporarily stores an AccountTransaction inside the Loan until the next payment execution, at which point principal balances are reduced accordingly.
/// </summary>
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