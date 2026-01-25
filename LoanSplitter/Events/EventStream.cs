namespace LoanSplitter.Events;

/// <summary>
/// Replays user events plus internally generated MaybeEvents to build immutable State snapshots.
/// Orders events by date and interleaves user-provided events with automatically generated ones (such as scheduled payments).
/// For each event:
/// 1. Calls Apply(State) to produce an EventOutcome
/// 2. Merges the returned StateUpdates into the previous state via state.WithUpdates(...), yielding a fresh immutable State
/// 3. Queues any emitted MaybeEvent for future execution
/// 
/// The process is purely functional: no event mutates an existing State. Historical timelines remain reproducible.
/// </summary>
public class EventStream
{
    private readonly List<(DateTime Date, State State)> _historicalStates;

    private readonly List<MaybeEvent> _maybeEvents;

    private readonly List<EventBase> _processedEvents;
    private readonly List<EventBase> _systemEvents;
    private readonly List<EventBase> _userEvents;
    private int _userEventsArrayIndex;

    public EventStream(List<EventBase> userEvents)
    {
        _userEvents = userEvents;
        var state = new State(new Dictionary<string, object>());

        _historicalStates = new List<(DateTime Date, State State)>();
        _maybeEvents = new List<MaybeEvent>();
        _processedEvents = new List<EventBase>();
        _systemEvents = new List<EventBase>();

        // Check if there is any pending event with an earlier date than the current event.

        var counter = 0;
        while (true)
        {
            var nextEvent = GetNextEvent(state);

            if (nextEvent.Event == null && !nextEvent.HasMore)
                // this is our exit condition. Otherwise we keep going.
                // todo add a date limit as well?
                break;

            if (nextEvent.Event != null) state = ProcessEvent(state, nextEvent.Event);

            if (counter++ == 5000) throw new ApplicationException("Too many events");
        }
    }

    private (EventBase? Event, bool HasMore) GetNextEvent(State state)
    {
        var nextUserEvent = _userEventsArrayIndex >= _userEvents.Count ? null : _userEvents[_userEventsArrayIndex];

        var eligibleMaybeEvent = _maybeEvents
            .FirstOrDefault(maybeEvent => nextUserEvent == null || maybeEvent.CheckDate < nextUserEvent.Date);

        var generatedEvent = eligibleMaybeEvent?.EventFactory(state);

        if (generatedEvent != null)
        {
            _systemEvents.Add(generatedEvent);
            return (generatedEvent, true);
        }

        if (eligibleMaybeEvent != null)
            // Remove the maybe eligible event that has been processed.
            _maybeEvents.Remove(eligibleMaybeEvent);

        if (nextUserEvent == null && _maybeEvents.Count == 0)
            // now we finished everything.
            return (null, false);

        // only consume now the user event.
        _userEventsArrayIndex++;
        return (nextUserEvent, true);
    }

    private State ProcessEvent(State state, EventBase ev)
    {
        var stateChange = ev.Apply(state);
        var newState = state.WithUpdates(stateChange.StateUpdates);

        if (stateChange.MaybeEvent != null) _maybeEvents.Add(stateChange.MaybeEvent);

        _processedEvents.Add(ev);
        _historicalStates.Add((ev.Date, newState));

        return newState;
    }

    public State? GetStateForDate(DateTime date)
    {
        if (_historicalStates.Count == 0) return null;

        for (var i = _historicalStates.Count - 1; i >= 0; i--)
            if (_historicalStates[i].Date <= date)
                return _historicalStates[i].State;

        return null;
    }

    public IEnumerable<EventBase> GetEventsUpToDate(DateTime date)
    {
        return _processedEvents.Where(e => e.Date <= date);
    }
}