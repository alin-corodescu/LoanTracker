namespace LoanSplitter.Events;

/// <summary>
/// Base class for all events.
/// </summary>
public abstract class EventBase(DateTime date)
{
    public DateTime Date { get; } = date;
    
    public abstract EventOutcome Apply(State state);
}

// Other things I need to support.

// Different contributions per person for one loan.