using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LoanSplitter.Events;

public class State
{
    private readonly Dictionary<string, object> _entities;
    private readonly IReadOnlyDictionary<string, object> _readOnlyEntities;

    public State(Dictionary<string, object> entities)
    {
        _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        _readOnlyEntities = new ReadOnlyDictionary<string, object>(_entities);
    }

    public IReadOnlyDictionary<string, object> Entities => _readOnlyEntities;

    public T GetEntityByName<T>(string entityName)
    {
        if (!_entities.TryGetValue(entityName, out var entity))
            throw new KeyNotFoundException($"Entity '{entityName}' was not found in the current state.");

        return (T)entity;
    }

    public State WithUpdates(Dictionary<string, object> updates)
    {
        ArgumentNullException.ThrowIfNull(updates);

        var newEntities = new Dictionary<string, object>(_entities);

        foreach (var update in updates) newEntities[update.Key] = update.Value;

        return new State(newEntities);
    }
}