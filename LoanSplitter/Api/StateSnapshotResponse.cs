using LoanSplitter.Domain;

namespace LoanSplitter.Api;

/// <summary>
/// Response for POST /eventStream/{id}/stateSnapshot.
/// Returns all state entities grouped by their type (loans, accounts, bills).
/// This provides a structured view of the complete state at a specific point in time.
/// </summary>
public sealed class StateSnapshotResponse
{
    public Dictionary<string, Loan> Loans { get; init; } = new();
    public Dictionary<string, Account> Accounts { get; init; } = new();
    public Dictionary<string, Bill> Bills { get; init; } = new();
    public DateTime SnapshotDate { get; init; }

    public static StateSnapshotResponse From(Events.State state, DateTime snapshotDate)
    {
        ArgumentNullException.ThrowIfNull(state);

        var response = new StateSnapshotResponse
        {
            SnapshotDate = snapshotDate
        };

        foreach (var (name, entity) in state.Entities)
        {
            switch (entity)
            {
                case Loan loan:
                    response.Loans[name] = loan;
                    break;
                case Account account:
                    response.Accounts[name] = account;
                    break;
                case Bill bill:
                    response.Bills[name] = bill;
                    break;
            }
        }

        return response;
    }
}
