using System.Text.Json;
using LoanSplitter.Api;
using LoanSplitter.Domain;
using LoanSplitter.Events;
using LoanSplitter.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "FrontendDevPolicy";

builder.Services.AddSingleton<UserEventJsonDeserializer>();
builder.Services.AddSingleton<IEventStreamStore, EventStreamStore>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors(CorsPolicyName);

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

app.MapGet("/eventStream/{eventStreamId:guid}/loanSummary",
        (Guid eventStreamId,
            [FromQuery(Name = "date")] DateTime? date,
            [FromQuery(Name = "loanName")] string? loanName,
            IEventStreamStore store) =>
        {
            if (date is null)
                return Results.BadRequest("The 'date' query parameter is required.");

            if (string.IsNullOrWhiteSpace(loanName))
                return Results.BadRequest("The 'loanName' query parameter is required.");

            if (!store.TryGet(eventStreamId, out var stream))
                return Results.NotFound(new { error = $"Event stream '{eventStreamId}' was not found." });

            var state = stream.GetStateForDate(date.Value);

            if (state is null)
                return Results.NotFound(new { error = $"No state available on or before {date:O}." });

            if (!state.Entities.TryGetValue(loanName, out var loanEntity) || loanEntity is not Loan loan)
                return Results.NotFound(new { error = $"Loan '{loanName}' was not found in the state." });

            var summary = LoanSummaryResponse.From(loanName, loan, date.Value);
            return Results.Ok(summary);
        })
    .WithName("GetLoanSummary")
    .Produces<LoanSummaryResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound);

app.MapGet("/eventStream/{eventStreamId:guid}/events",
        (Guid eventStreamId,
            [FromQuery(Name = "date")] DateTime? date,
            IEventStreamStore store,
            UserEventJsonDeserializer deserializer) =>
        {
            if (date is null) return Results.BadRequest("The 'date' query parameter is required.");

            if (!store.TryGet(eventStreamId, out var stream))
                return Results.NotFound(new { error = $"Event stream '{eventStreamId}' was not found." });

            var events = stream.GetEventsUpToDate(date.Value);

            // We use the deserializer to ensure correct polymorphic serialization
            var json = deserializer.Serialize(events);
            return Results.Content(json, "application/json");
        })
    .WithName("GetEventStreamEvents")
    .Produces<IEnumerable<EventBase>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound);

app.Run();

public partial class Program
{
}