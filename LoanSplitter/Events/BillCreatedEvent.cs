using LoanSplitter.Domain;

namespace LoanSplitter.Events;

/// <summary>
/// Create a bill to track expenditures for participants. Can be used for any type of spending (groceries, utilities, loan payments, etc.).
/// Stores the Bill entity in state and updates the shared PersonBalances so that each participant who did not pay
/// records a debt to the payer for their share of the bill.
/// </summary>
public class BillCreatedEvent(DateTime date, string billName, string accountName, Bill bill)
    : EventBase(date)
{
    public string BillName { get; } = billName ?? throw new ArgumentNullException(nameof(billName));
    /// <summary>The account associated with the bill (retained for informational/serialization purposes).</summary>
    public string AccountName { get; } = accountName ?? throw new ArgumentNullException(nameof(accountName));
    public Bill Bill { get; } = bill ?? throw new ArgumentNullException(nameof(bill));

    public override EventOutcome Apply(State state)
        => new(ComputeStateUpdates(state, BillName, Bill));

    /// <summary>
    /// Computes the state updates for adding a bill: stores the bill entity and updates PersonBalances with the resulting debts.
    /// Use this to apply bill side-effects directly from other events without creating an inline BillCreatedEvent.
    /// </summary>
    public static Dictionary<string, object> ComputeStateUpdates(State state, string billName, Bill bill)
    {
        var balances = state.GetEntityByName<PersonBalances>(PersonBalances.StateKey);
        var updatedBalances = balances;
        foreach (var (debtor, creditor, amount) in bill.ComputeDebts())
            updatedBalances = updatedBalances.WithDebt(debtor, creditor, amount);

        return new Dictionary<string, object>
        {
            { billName, bill },
            { PersonBalances.StateKey, updatedBalances }
        };
    }
}

/// <summary>
/// Legacy event for backwards compatibility. Creates a bill from individual parameters.
/// Stores the Bill entity in state and updates PersonBalances with debts derived from the bill's shares.
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
    /// <summary>The account associated with the bill (retained for informational/serialization purposes).</summary>
    public string AccountName { get; }
    public string PaidByAccount { get; }
    public IReadOnlyDictionary<string, double> ForAccountsWithShares { get; }

    public override EventOutcome Apply(State state)
    {
        var bill = new Bill(Description, Items, PaidByAccount, new Dictionary<string, double>(ForAccountsWithShares));

        var balances = state.GetEntityByName<PersonBalances>(PersonBalances.StateKey);
        var updatedBalances = balances;
        foreach (var (debtor, creditor, amount) in bill.ComputeDebts())
            updatedBalances = updatedBalances.WithDebt(debtor, creditor, amount);

        return new EventOutcome(new Dictionary<string, object>
        {
            { BillName, bill },
            { PersonBalances.StateKey, updatedBalances }
        });
    }
}
