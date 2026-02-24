using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LoanSplitter.Domain;

public class Bill
{
    private readonly List<BillItem> _items;
    private readonly Dictionary<string, double> _forAccountsWithShares;
    
    /// <summary>
    /// Creates a bill with detailed itemization.
    /// </summary>
    public Bill(string description, IEnumerable<BillItem> items, string paidByAccount, Dictionary<string, double> forAccountsWithShares)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        _items = items != null ? new List<BillItem>(items) : new List<BillItem>();
        PaidByAccount = paidByAccount ?? throw new ArgumentNullException(nameof(paidByAccount));
        _forAccountsWithShares = new Dictionary<string, double>(forAccountsWithShares ?? throw new ArgumentNullException(nameof(forAccountsWithShares)));
        
        if (_forAccountsWithShares.Count == 0)
            throw new ArgumentException("ForAccountsWithShares must contain at least one account.", nameof(forAccountsWithShares));
            
        var shareSum = _forAccountsWithShares.Values.Sum();
        if (Math.Abs(shareSum - 1.0) > 0.0001)
            throw new ArgumentException($"Account shares must sum to 1.0, but sum is {shareSum}.", nameof(forAccountsWithShares));
        
        // Calculate total from items if present
        TotalAmount = _items.Count > 0 ? _items.Sum(item => item.Amount) : 0;
    }
    
    /// <summary>
    /// Creates a bill without itemization, using only total amount and shares.
    /// </summary>
    public Bill(string description, double totalAmount, string paidByAccount, Dictionary<string, double> forAccountsWithShares)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        _items = new List<BillItem>();
        TotalAmount = totalAmount;
        PaidByAccount = paidByAccount ?? throw new ArgumentNullException(nameof(paidByAccount));
        _forAccountsWithShares = new Dictionary<string, double>(forAccountsWithShares ?? throw new ArgumentNullException(nameof(forAccountsWithShares)));
        
        if (_forAccountsWithShares.Count == 0)
            throw new ArgumentException("ForAccountsWithShares must contain at least one account.", nameof(forAccountsWithShares));
            
        var shareSum = _forAccountsWithShares.Values.Sum();
        if (Math.Abs(shareSum - 1.0) > 0.0001)
            throw new ArgumentException($"Account shares must sum to 1.0, but sum is {shareSum}.", nameof(forAccountsWithShares));
    }

    public string Description { get; }
    public string PaidByAccount { get; }
    public IReadOnlyDictionary<string, double> ForAccountsWithShares => _forAccountsWithShares;
    public IReadOnlyList<BillItem> Items => new ReadOnlyCollection<BillItem>(_items);

    public double TotalAmount { get; }

    public static Bill CreateSplitBill(string description, string category, double totalAmount, Dictionary<string, double> shares, string paidByAccount)
    {
        if (shares == null || shares.Count == 0)
            throw new ArgumentException("Shares dictionary must contain at least one entry.", nameof(shares));

        var shareSum = shares.Values.Sum();
        if (Math.Abs(shareSum - 1.0) > 0.0001)
            throw new ArgumentException($"Shares must sum to 1.0, but sum is {shareSum}.", nameof(shares));

        var items = shares.Select(kvp => new BillItem(totalAmount * kvp.Value, kvp.Key, category)).ToList();
        
        return new Bill(description, items, paidByAccount, shares);
    }

    /// <summary>
    /// Computes the debts this bill creates: each person in ForAccountsWithShares (except PaidByAccount) owes PaidByAccount their share.
    /// </summary>
    public IEnumerable<(string Debtor, string Creditor, double Amount)> ComputeDebts()
    {
        if (_items.Count > 0)
        {
            return _items
                .Where(i => i.PersonName != PaidByAccount)
                .GroupBy(i => i.PersonName)
                .Select(g => (Debtor: g.Key, Creditor: PaidByAccount, Amount: g.Sum(i => i.Amount)));
        }

        return _forAccountsWithShares
            .Where(s => s.Key != PaidByAccount)
            .Select(s => (Debtor: s.Key, Creditor: PaidByAccount, Amount: TotalAmount * s.Value));
    }

    public Account ApplyToAccount(Account account)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account));

        var updatedAccount = account;
        
        // If we have itemized bills, use those
        if (Items.Count > 0)
        {
            foreach (var item in Items)
            {
                var transaction = new AccountTransaction(item.Amount, item.PersonName);
                updatedAccount = updatedAccount.WithTransaction(transaction);
            }
        }
        else
        {
            // Otherwise, create transactions based on shares and total amount
            foreach (var share in ForAccountsWithShares)
            {
                var amount = TotalAmount * share.Value;
                var transaction = new AccountTransaction(amount, share.Key);
                updatedAccount = updatedAccount.WithTransaction(transaction);
            }
        }

        return updatedAccount;
    }
}

public class BillItem
{
    public BillItem(double amount, string personName, string category)
    {
        Amount = amount;
        PersonName = personName ?? throw new ArgumentNullException(nameof(personName));
        Category = category ?? throw new ArgumentNullException(nameof(category));
    }

    // Legacy constructor for backwards compatibility
    public BillItem(double amount, string itemName)
        : this(amount, itemName, "General")
    {
    }

    public double Amount { get; }
    public string PersonName { get; }
    public string Category { get; }
    
    // Alias for backwards compatibility
    public string ItemName => PersonName;
}
