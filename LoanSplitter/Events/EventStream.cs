using LoanSplitter.Domain;

namespace LoanSplitter.Events;

public class EventStream
{
    private List<EventBase> _userEvents;

    private List<(DateTime Date, State State)> _historicalStates;
    
    private List<MaybeEvent> _maybeEvents;
    private int _userEventsArrayIndex;
    
    private List<EventBase> _systemEvents;
    

    public EventStream(List<EventBase> userEvents)
    {
        _userEvents = userEvents;
        var state = new State(new Dictionary<string, object>());
        
        this._historicalStates = new List<(DateTime Date, State State)>();
        this._maybeEvents = new List<MaybeEvent>();
        this._systemEvents = new List<EventBase>();
        
        // Check if there is any pending event with an earlier date than the current event.

        int counter = 0;
        while (true)
        {
            var nextEvent = this.GetNextEvent(state);

            if (nextEvent.Event == null && !nextEvent.HasMore)
            {
                // this is our exit condition. Otherwise we keep going.
                // todo add a date limit as well?
                break;
            }
            
            if (nextEvent.Event != null)
            {
                state = this.ProcessEvent(state, nextEvent.Event);
            }

            if (counter++ == 1000)
            {
                throw new ApplicationException("Too many events");
            }
        }
    }

    private (EventBase? Event, bool HasMore) GetNextEvent(State state)
    {
        var nextUserEvent = _userEventsArrayIndex >= _userEvents.Count ? null : _userEvents[_userEventsArrayIndex];
        
        var eligibleMaybeEvent = this._maybeEvents
            .FirstOrDefault(maybeEvent => nextUserEvent == null || maybeEvent.CheckDate < nextUserEvent.Date);
        
        var generatedEvent = eligibleMaybeEvent?.EventFactory(state);
        
        if (generatedEvent != null)
        {
            this._systemEvents.Add(generatedEvent);
            return (generatedEvent, true);
        }

        if (eligibleMaybeEvent != null)
        {
            // Remove the maybe eligible event that has been processed.
            this._maybeEvents.Remove(eligibleMaybeEvent);
        }

        if (nextUserEvent == null && this._maybeEvents.Count == 0)
        {
            // now we finished everything.
            return (null, false);
        }
        
        // only consume now the user event.
        _userEventsArrayIndex++;
        return (nextUserEvent, true);
    }

    private State ApplyEligibleMaybeEvents(State state, EventBase upcomingUserEvent)
    {
        var eligibleMaybeEvent = this._maybeEvents
            .FirstOrDefault(maybeEvent => maybeEvent.CheckDate < upcomingUserEvent.Date);

        var generatedEvent = eligibleMaybeEvent?.EventFactory(state);
        
        if (generatedEvent != null)
        {
            state = this.ProcessEvent(state, generatedEvent);
        }

        // Remove the event if it has been processed.
        if (eligibleMaybeEvent != null) this._maybeEvents.Remove(eligibleMaybeEvent);

        return state;
    }

    private State ProcessEvent(State state, EventBase ev)
    {
        var stateChange = ev.Apply(state);
        var newState = state.WithUpdates(stateChange.StateUpdates);

        if (stateChange.MaybeEvent != null)
        {
            this._maybeEvents.Add(stateChange.MaybeEvent);      
        }
        
        this._historicalStates.Add((ev.Date, newState));
        
        return newState;
    }
}