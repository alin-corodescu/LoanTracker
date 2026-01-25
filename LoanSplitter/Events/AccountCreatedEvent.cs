using LoanSplitter.Domain;

namespace LoanSplitter.Events;

/// <summary>
/// Bootstrap a staging account before any money flows.
/// Adds a fresh Account instance under the provided account name.
/// </summary>
public class AccountCreatedEvent(DateTime date, string acctName) : EventBase(date)
{
    public string AccountName { get; } = acctName;

    public override EventOutcome Apply(State state)
    {
        return new EventOutcome(new Dictionary<string, object>
        {
            { AccountName, new Account() }
        });
    }
}