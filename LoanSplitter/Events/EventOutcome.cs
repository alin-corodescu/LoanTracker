namespace LoanSplitter.Events;

/// <summary>
/// Result of applying an event to a state.
/// Contains state updates and an optional future event (MaybeEvent).
/// </summary>
public class EventOutcome(Dictionary<string, object> stateUpdates, MaybeEvent? maybeEvent = null)
{
    public Dictionary<string, object> StateUpdates { get; } = stateUpdates;

    public MaybeEvent? MaybeEvent { get; } = maybeEvent;
}