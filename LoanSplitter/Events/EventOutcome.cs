namespace LoanSplitter.Events;

public class EventOutcome(Dictionary<string, object> stateUpdates, MaybeEvent? maybeEvent = null)
{
    public Dictionary<string, object> StateUpdates { get; } = stateUpdates;
    
    public MaybeEvent? MaybeEvent { get; } = maybeEvent;
}