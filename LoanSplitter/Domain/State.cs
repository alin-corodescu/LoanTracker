namespace LoanSplitter.Events;

public class State(Dictionary<string, object> entities)
{
    public T GetEntityByName<T>(string entityName)
    {
        return (T)entities[entityName];
    }

    public State WithUpdates(Dictionary<string, object> updates)
    {
        var newEntities = new Dictionary<string, object>(updates);

        foreach (var existing in entities)
        {
            if (newEntities.ContainsKey(existing.Key)) continue;

            newEntities[existing.Key] = existing.Value;
        }

        return new State(newEntities);
    }
}