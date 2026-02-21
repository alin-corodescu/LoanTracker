using LoanSplitter.Domain;

namespace LoanSplitter.Events;

/// <summary>
/// Create a bill to track expenditures for participants. Can be used for any type of spending (groceries, utilities, loan payments, etc.).
/// Takes a Bill entity and updates the associated Account by invoking the bill's ApplyToAccount() method, which creates transactions for each bill item.
/// </summary>
public class BillCreatedEvent(DateTime date, string billName, string accountName, Bill bill)
    : EventBase(date)
{
    public string BillName { get; } = billName ?? throw new ArgumentNullException(nameof(billName));
    public string AccountName { get; } = accountName ?? throw new ArgumentNullException(nameof(accountName));
    public Bill Bill { get; } = bill ?? throw new ArgumentNullException(nameof(bill));

    public override EventOutcome Apply(State state)
    {
        var account = state.GetEntityByName<Account>(AccountName);
        var updatedAccount = Bill.ApplyToAccount(account);

        var updates = new Dictionary<string, object>
        {
            { BillName, Bill },
            { AccountName, updatedAccount }
        };

        return new EventOutcome(updates, null);
    }
}

/// <summary>
/// Legacy event for backwards compatibility. Creates a bill from individual parameters.
/// Adds a Bill entity and updates the associated Account by invoking the bill's ApplyToAccount() method, which creates transactions for each bill item.
/// </summary>
public class BillAddedEvent : EventBase
{
    public BillAddedEvent(DateTime date, string billName, string description, IEnumerable<BillItem> items, string accountName, string paidByAccount, Dictionary<string, double> forAccountsWithShares)
        : base(date)
    {
        BillName = billName ?? throw new ArgumentNullException(nameof(billName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Items = new List<BillItem>(items ?? throw new ArgumentNullException(nameof(items)));
        AccountName = accountName ?? throw new ArgumentNullException(nameof(accountName));
        PaidByAccount = paidByAccount ?? throw new ArgumentNullException(nameof(paidByAccount));
        ForAccountsWithShares = new Dictionary<string, double>(forAccountsWithShares ?? throw new ArgumentNullException(nameof(forAccountsWithShares)));
        
        if (ForAccountsWithShares.Count == 0)
            throw new ArgumentException("ForAccountsWithShares must contain at least one account.", nameof(forAccountsWithShares));
    }

    public string BillName { get; }
    public string Description { get; }
    public IReadOnlyList<BillItem> Items { get; }
    public string AccountName { get; }
    public string PaidByAccount { get; }
    public IReadOnlyDictionary<string, double> ForAccountsWithShares { get; }

    public override EventOutcome Apply(State state)
    {
        var bill = new Bill(Description, Items, PaidByAccount, new Dictionary<string, double>(ForAccountsWithShares));
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
