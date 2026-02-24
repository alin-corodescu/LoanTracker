using System.Collections.Generic;
using System.Collections.ObjectModel;
using LoanSplitter.Domain;

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
        {
            // Auto-create Account entities on first reference
            if (typeof(T) == typeof(Account))
            {
                var newAccount = new Account();
                _entities[entityName] = newAccount;
                return (T)(object)newAccount;
            }

            // Auto-create PersonBalances on first reference
            if (typeof(T) == typeof(PersonBalances))
            {
                var newBalances = new PersonBalances();
                _entities[entityName] = newBalances;
                return (T)(object)newBalances;
            }
            
            throw new KeyNotFoundException($"Entity '{entityName}' was not found in the current state.");
        }

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