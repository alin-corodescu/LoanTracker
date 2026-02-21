/// <summary>
/// LoanSplitter API - ASP.NET Core minimal API that simulates event streams in memory.
/// 
/// Endpoints:
/// - POST /eventStream - Accepts a JSON array of user events and returns a stream ID
/// - GET /eventStream/{id}?date=YYYY-MM-DD - Returns immutable state snapshot at a specific date
/// - GET /eventStream/{id}/loanSummary?date=YYYY-MM-DD&loanName=... - Returns computed loan summary
/// - GET /eventStream/{id}/events?date=YYYY-MM-DD - Returns all events up to a specific date
/// 
/// States are stored purely in memory; restart the service to clear all streams.
/// CORS is wide open for development so the static frontend can call the API without extra proxying.
/// 
/// Run locally: dotnet run --project LoanSplitter/LoanSplitter.csproj
/// </summary>
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

/// <summary>
/// POST /eventStream
/// 
/// Accepts a JSON array of user events that follows the schema handled by UserEventJsonDeserializer.
/// Each object must contain a "type" discriminator, "date", and any type-specific fields.
/// The server materializes the events into an EventStream, stores it in memory, and returns a unique identifier.
/// 
/// Example:
/// curl -X POST "http://localhost:5000/eventStream" \
///   -H "Content-Type: application/json" \
///   -d '[{ "type": "LoanContracted", "date": "2025-10-15", "loanName": "loan1", "principal": 100000, "nominalRate": 4.5, "term": 360, "backingAccountName": "creditAcct", "name1": "Alin", "name2": "Diana" }]'
/// 
/// Response: { "id": "f6c3f219-93b6-4035-a169-8bb2f1df002f" }
/// </summary>
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

/// <summary>
/// GET /eventStream/{id}?date=YYYY-MM-DD
/// 
/// Returns the immutable State snapshot produced by the stream on or before the requested date.
/// If the stream or date can't be satisfied, returns 404.
/// 
/// Example:
/// curl "http://localhost:5000/eventStream/f6c3f219-93b6-4035-a169-8bb2f1df002f?date=2026-06-01"
/// </summary>
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

/// <summary>
/// GET /eventStream/{id}/loanSummary?date=YYYY-MM-DD&loanName=apartLoan
/// 
/// Returns a computed summary for a specific loan at the requested date, including:
/// - Next payment split by person
/// - Remaining principal and projected interest totals (both aggregated and per borrower)
/// 
/// This endpoint powers the frontend helper.
/// 
/// Example:
/// curl "http://localhost:5000/eventStream/{id}/loanSummary?date=2026-06-01&loanName=apartLoan"
/// </summary>
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

/// <summary>
/// POST /eventStream/{id}/stateSnapshot
/// 
/// Returns the complete state at a specific point in time, grouped by entity types.
/// Accepts a JSON body with a cutoffDate field.
/// 
/// Example:
/// curl -X POST "http://localhost:5000/eventStream/{id}/stateSnapshot" \
///   -H "Content-Type: application/json" \
///   -d '{ "cutoffDate": "2026-06-01" }'
/// 
/// Response includes all loans, accounts, and bills present in the state at that date.
/// </summary>
app.MapPost("/eventStream/{eventStreamId:guid}/stateSnapshot",
        (Guid eventStreamId,
            StateSnapshotRequest request,
            IEventStreamStore store) =>
        {
            if (!store.TryGet(eventStreamId, out var stream))
                return Results.NotFound(new { error = $"Event stream '{eventStreamId}' was not found." });

            var state = stream.GetStateForDate(request.CutoffDate);

            if (state is null)
                return Results.NotFound(new { error = $"No state available on or before {request.CutoffDate:O}." });

            var response = StateSnapshotResponse.From(state, request.CutoffDate);
            return Results.Ok(response);
        })
    .WithName("GetStateSnapshot")
    .Produces<StateSnapshotResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound);

app.Run();

public partial class Program
{
}