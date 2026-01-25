using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LoanSplitter.Events;

namespace LoanSplitter.Services;

/// <summary>
/// In-memory catalog that keeps uploaded event streams accessible via the API.
/// Caches streams keyed by GUID. No persistence - restart the service to clear all streams.
/// </summary>
public interface IEventStreamStore
{
    Guid Add(IEnumerable<EventBase> events);

    bool TryGet(Guid id, out EventStream stream);
}

public sealed class EventStreamStore : IEventStreamStore
{
    private readonly ConcurrentDictionary<Guid, EventStream> _streams = new();

    public Guid Add(IEnumerable<EventBase> events)
    {
        if (events is null) throw new ArgumentNullException(nameof(events));

        var eventList = events as List<EventBase> ?? events.ToList();
        var stream = new EventStream(eventList);
        var id = Guid.NewGuid();

        _streams[id] = stream;
        return id;
    }

    public bool TryGet(Guid id, out EventStream stream)
    {
        if (_streams.TryGetValue(id, out var existing))
        {
            stream = existing;
            return true;
        }

        stream = null!;
        return false;
    }
}
