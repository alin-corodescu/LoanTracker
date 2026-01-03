using System.Text.Json;
using LoanSplitter.Events;
using LoanSplitter.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<UserEventJsonDeserializer>();
builder.Services.AddSingleton<IEventStreamStore, EventStreamStore>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { message = "LoanSplitter API is running" }));

app.MapPost("/eventStream", async Task<IResult> (
        HttpRequest request,
        UserEventJsonDeserializer deserializer,
        IEventStreamStore store) =>
    {
        using var reader = new StreamReader(request.Body);
        var payload = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(payload)) return Results.BadRequest("Request body cannot be empty.");

        List<EventBase> events;

        try
        {
            events = deserializer.Deserialize(payload);
        }
        catch (JsonException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }

        var id = store.Add(events);

        return Results.Created($"/eventStream/{id}", new { id });
    })
    .WithName("CreateEventStream")
    .Produces(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest);

app.MapGet("/eventStream/{eventStreamId:guid}",
        (Guid eventStreamId,
            [FromQuery(Name = "date")] DateTime? date,
            IEventStreamStore store) =>
        {
            if (date is null) return Results.BadRequest("The 'date' query parameter is required.");

            if (!store.TryGet(eventStreamId, out var stream))
                return Results.NotFound(new { error = $"Event stream '{eventStreamId}' was not found." });

            var state = stream.GetStateForDate(date.Value);

            return state is null
                ? Results.NotFound(new { error = $"No state available on or before {date:O}." })
                : Results.Ok(state);
        })
    .WithName("GetEventStreamState")
    .Produces<State>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound);

app.Run();

public partial class Program
{
}