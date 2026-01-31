namespace LoanSplitter.Api;

/// <summary>
/// Request body for POST /eventStream/{id}/stateSnapshot.
/// Specifies the cutoff date for which to return the state.
/// </summary>
public sealed class StateSnapshotRequest
{
    public DateTime CutoffDate { get; init; }
}
