namespace LoanSplitter.Events;

/// <summary>
/// Result of applying an event to a state.
/// Contains state updates, an optional future event (MaybeEvent), and optional inline events to process immediately.
/// </summary>
public class EventOutcome(Dictionary<string, object> stateUpdates, MaybeEvent? maybeEvent = null, List<EventBase>? inlineEvents = null)
{
    public Dictionary<string, object> StateUpdates { get; } = stateUpdates;

    public MaybeEvent? MaybeEvent { get; } = maybeEvent;
    
    public List<EventBase>? InlineEvents { get; } = inlineEvents;
}