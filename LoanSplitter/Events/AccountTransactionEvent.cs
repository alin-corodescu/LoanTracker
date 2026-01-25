using LoanSplitter.Domain;

namespace LoanSplitter.Events;

/// <summary>
/// Record a manual deposit/withdrawal that is not part of the automated loan flow.
/// Appends an AccountTransaction to the chosen Account.
/// </summary>
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