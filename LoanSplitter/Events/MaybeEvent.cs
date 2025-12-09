namespace LoanSplitter.Events;

public class MaybeEvent(DateTime checkDate, Func<State, EventBase?> eventFactory)
{
    public DateTime CheckDate { get; } = checkDate;
    public Func<State, EventBase?> EventFactory { get; } = eventFactory;
}