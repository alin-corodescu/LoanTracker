namespace LoanSplitter.Events;

public class EventStream
{
    private readonly List<(DateTime Date, State State)> _historicalStates;

    private readonly List<MaybeEvent> _maybeEvents;

    private readonly List<EventBase> _systemEvents;
    private readonly List<EventBase> _userEvents;
    private int _userEventsArrayIndex;

    public EventStream(List<EventBase> userEvents)
    {
        _userEvents = userEvents;
        var state = new State(new Dictionary<string, object>());

        _historicalStates = new List<(DateTime Date, State State)>();
        _maybeEvents = new List<MaybeEvent>();
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
}