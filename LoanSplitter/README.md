# LoanSplitter API

LoanSplitter now runs as an ASP.NET Core minimal API that simulates event streams in memory and lets you inspect the resulting state at any point in time.

## Run the service locally

```bash
dotnet run --project LoanSplitter/LoanSplitter.csproj
```

The app listens on the default ASP.NET Core ports (usually `http://localhost:5000` / `https://localhost:5001`).

## Endpoints

### POST `/eventStream`

Accepts a JSON array of user events that follows the schema handled by `UserEventJsonDeserializer` (each object must contain a `type` discriminator, `date`, and any type-specific fields). The server materializes the events into an `EventStream`, stores it in memory, and returns a unique identifier.

**Response**

```json
{
	"id": "f6c3f219-93b6-4035-a169-8bb2f1df002f"
}
```

**Example**

```bash
curl -X POST "http://localhost:5000/eventStream" \
	-H "Content-Type: application/json" \
	-d '[
				{ "type": "AccountCreated", "date": "2025-06-01", "acctName": "creditAcct" },
				{ "type": "LoanContracted", "date": "2025-11-01", "loanName": "apartLoan", "principal": 8150100, "nominalRate": 4.74, "term": 360, "backingAccountName": "creditAcct", "name1": "Alin", "name2": "Diana" }
			]'
```

### GET `/eventStream/{id}?date=YYYY-MM-DD`

Returns the immutable `State` snapshot produced by the stream on or before the requested date. If the stream or date can’t be satisfied, the API returns `404` with an error payload.

**Example**

```bash
curl "http://localhost:5000/eventStream/f6c3f219-93b6-4035-a169-8bb2f1df002f?date=2026-06-01"
```

**Response shape**

```json
{
	"entities": {
		"apartLoan": {
			"remainingAmount": 6398607.23,
			"remainingTermInMonths": 356,
			"totalFeesPaid": 65,
			"totalInterestPaid": 40764,
			"subLoans": {
				"Alin": {
					"remainingAmount": 3199303.61,
					"remainingTermInMonths": 356
				},
				"Diana": {
					"remainingAmount": 3199303.61,
					"remainingTermInMonths": 356
				}
			}
		}
	}
}
```

## Notes

- States are stored purely in memory; restart the service to clear all streams.
- The GET endpoint returns `404` when the date is earlier than the first event or when the stream id does not exist.
- `State.Entities` is serialized with camel-cased property names for easier client consumption.

## Project layout

The repository is intentionally small, but each folder plays a specific role:

- `Domain/` – immutable domain models such as `Loan`, `Account`, `AccountTransaction`, and the aggregate `State` object that represents the world at a given point in the stream.
- `Events/` – the event-sourced heart of the system, containing every `EventBase` subtype, the `EventStream` runner, and the `UserEventJsonDeserializer` that handles JSON import/export of events.
- `Services/` – currently just `EventStreamStore`, an in-memory catalog that keeps the uploaded event streams accessible via the API.
- `Program.cs` & `appsettings*.json` – the ASP.NET Core minimal API host and configuration; this wires up dependency injection and defines the `/eventStream` endpoints described above.
- `LoanSplitter.Tests/` (sibling project) – MSTest-based unit tests that exercise event logic, JSON serialization, and the event stream behavior.

Feel free to explore or extend any of these areas depending on whether you're modeling new loan behaviors, introducing additional events, or building richer API surfaces.

For a deeper look at the business entities and events the system models, see [`DOMAIN.md`](./DOMAIN.md).

