namespace LoanSplitter.Domain;

/// <summary>
/// Tracks net balances between accounts and persons, accumulated from bills.
/// A balance entry (debtor -> creditor -> amount) means the debtor owes the creditor that net amount.
/// Immutable: use WithDebt() to produce updated instances.
/// Stored in state under <see cref="StateKey"/>.
/// </summary>
public class PersonBalances
{
    public const string StateKey = "Balances";

    // debtor -> creditor -> net amount owed
    private readonly Dictionary<string, Dictionary<string, double>> _balances;

    public PersonBalances()
    {
        _balances = new Dictionary<string, Dictionary<string, double>>();
    }

    private PersonBalances(Dictionary<string, Dictionary<string, double>> balances)
    {
        _balances = balances;
    }

    /// <summary>
    /// Returns the net balances as a read-only view: debtor -> creditor -> amount owed.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Balances =>
        _balances.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyDictionary<string, double>)new Dictionary<string, double>(kvp.Value));

    /// <summary>
    /// Returns a new PersonBalances with the specified debt added to the running total.
    /// </summary>
    public PersonBalances WithDebt(string debtor, string creditor, double amount)
    {
        var newBalances = _balances.ToDictionary(
            kvp => kvp.Key,
            kvp => new Dictionary<string, double>(kvp.Value));

        if (!newBalances.TryGetValue(debtor, out var debtorBalances))
        {
            debtorBalances = new Dictionary<string, double>();
            newBalances[debtor] = debtorBalances;
        }

        debtorBalances[creditor] = debtorBalances.GetValueOrDefault(creditor) + amount;
        return new PersonBalances(newBalances);
    }
}
