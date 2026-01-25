using LoanSplitter.Domain;

namespace LoanSplitter.Events;

/// <summary>
/// Create a bill to track expenditures for participants. Can be used for any type of spending (groceries, utilities, loan payments, etc.).
/// Adds a Bill entity and updates the associated Account by invoking the bill's ApplyToAccount() method, which creates transactions for each bill item.
/// </summary>
public class BillCreatedEvent : EventBase
{
    public BillCreatedEvent(DateTime date, string billName, string description, IEnumerable<BillItem> items, string accountName)
        : base(date)
    {
        BillName = billName ?? throw new ArgumentNullException(nameof(billName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Items = new List<BillItem>(items ?? throw new ArgumentNullException(nameof(items)));
        AccountName = accountName ?? throw new ArgumentNullException(nameof(accountName));
    }

    public string BillName { get; }
    public string Description { get; }
    public IReadOnlyList<BillItem> Items { get; }
    public string AccountName { get; }

    public override EventOutcome Apply(State state)
    {
        var bill = new Bill(Description, Items, Date);
        var account = state.GetEntityByName<Account>(AccountName);
        
        var updatedAccount = bill.ApplyToAccount(account);

        var updates = new Dictionary<string, object>
        {
            { BillName, bill },
            { AccountName, updatedAccount }
        };

        return new EventOutcome(updates, null);
    }
}
