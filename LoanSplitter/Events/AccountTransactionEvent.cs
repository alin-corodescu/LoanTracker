using LoanSplitter.Domain;

namespace LoanSplitter.Events;

public class AccountTransactionEvent(DateTime date, string acctName, AccountTransaction transaction)
    : EventBase(date)
{
    public string AccountName { get; } = acctName;
    public AccountTransaction Transaction { get; } = transaction;

    public override EventOutcome Apply(State state)
    {
        var acct = state.GetEntityByName<Account>(AccountName);

        var updatedAcct = acct.WithTransaction(Transaction);

        return new EventOutcome(new Dictionary<string, object> { { AccountName, updatedAcct } });
    }
}