using LoanSplitter.Domain;

namespace LoanSplitter.Events;

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