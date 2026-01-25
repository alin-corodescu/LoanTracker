using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LoanSplitter.Domain;

public class Bill
{
    private readonly List<BillItem> _items;
    
    public Bill(string description, IEnumerable<BillItem> items, DateTime date)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        _items = new List<BillItem>(items ?? throw new ArgumentNullException(nameof(items)));
        Date = date;
    }

    public string Description { get; }
    public DateTime Date { get; }
    public IReadOnlyList<BillItem> Items => new ReadOnlyCollection<BillItem>(_items);

    public double TotalAmount => _items.Sum(item => item.Amount);

    public static Bill CreateSplitBill(string description, DateTime date, string category, double totalAmount, Dictionary<string, double> shares)
    {
        if (shares == null || shares.Count == 0)
            throw new ArgumentException("Shares dictionary must contain at least one entry.", nameof(shares));

        var shareSum = shares.Values.Sum();
        if (Math.Abs(shareSum - 1.0) > 0.0001)
            throw new ArgumentException($"Shares must sum to 1.0, but sum is {shareSum}.", nameof(shares));

        var items = shares.Select(kvp => new BillItem(totalAmount * kvp.Value, kvp.Key, category)).ToList();
        
        return new Bill(description, items, date);
    }

    public Account ApplyToAccount(Account account)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account));

        var updatedAccount = account;
        foreach (var item in Items)
        {
            var transaction = new AccountTransaction(item.Amount, item.PersonName);
            updatedAccount = updatedAccount.WithTransaction(transaction);
        }

        return updatedAccount;
    }
}
