namespace LoanSplitter.Domain;

/// <summary>
/// Tracks net balances between pairs of accounts.
/// Each pair (A, B) has exactly one entry with a signed amount:
///   positive → AccountA owes AccountB
///   negative → AccountB owes AccountA
/// The pair key is always stored with the alphabetically smaller account name first.
/// Immutable: use WithDebt() to produce updated instances.
/// Stored in state under <see cref="StateKey"/>.
/// </summary>
public class AccountBalances
{
    public const string StateKey = "Balances";

    // Normalized key (alphabetically first, second) -> signed net amount
    private readonly Dictionary<(string, string), double> _balances;

    public AccountBalances()
    {
        _balances = new Dictionary<(string, string), double>();
    }

    private AccountBalances(Dictionary<(string, string), double> balances)
    {
        _balances = balances;
    }

    // Always store pair as (alphabetically smaller, larger) to avoid duplicates
    private static (string, string) NormalizeKey(string a, string b) =>
        string.Compare(a, b, StringComparison.Ordinal) <= 0 ? (a, b) : (b, a);

    /// <summary>
    /// All pair balances. For each entry: positive Amount means AccountA owes AccountB,
    /// negative means AccountB owes AccountA.
    /// </summary>
    public IReadOnlyList<PairBalance> Balances =>
        _balances.Select(kvp => new PairBalance(kvp.Key.Item1, kvp.Key.Item2, kvp.Value)).ToList();

    /// <summary>
    /// Returns a new AccountBalances with the specified debt applied.
    /// Adding a debt in the opposite direction of an existing balance cancels it out.
    /// </summary>
    public AccountBalances WithDebt(string debtor, string creditor, double amount)
    {
        var newBalances = new Dictionary<(string, string), double>(_balances);
        var key = NormalizeKey(debtor, creditor);
        var sign = key.Item1 == debtor ? 1.0 : -1.0;

        var updated = newBalances.GetValueOrDefault(key, 0.0) + sign * amount;

        if (Math.Abs(updated) < 0.0001)
            newBalances.Remove(key);
        else
            newBalances[key] = updated;

        return new AccountBalances(newBalances);
    }
}

/// <summary>
/// Represents the net balance between two accounts.
/// Amount > 0: AccountA owes AccountB. Amount < 0: AccountB owes AccountA.
/// </summary>
public record PairBalance(string AccountA, string AccountB, double Amount)
{
    public string Debtor => Amount >= 0 ? AccountA : AccountB;
    public string Creditor => Amount >= 0 ? AccountB : AccountA;
    public double NetAmount => Math.Abs(Amount);
}