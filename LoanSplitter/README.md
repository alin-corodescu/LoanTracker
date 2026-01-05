# LoanSplitter API

LoanSplitter now runs as an ASP.NET Core minimal API that simulates event streams in memory and lets you inspect the resulting state at any point in time.

## UX jobs supported

The API plus the static frontend cover a few high-value “jobs to be done” for couples sharing a mortgage or similar long-term debt:

- **Import a historical timeline** – upload a JSON file with the exact events the bank provided (account creation, loan contract, advance payments, manual corrections, rate changes). The backend persists the event stream and gives you a stable ID that can be reused later.
- **Answer “Where do we stand today?”** – at any date, retrieve the immutable state to see each borrower’s remaining principal, fees, and interest paid so far.
- **Plan the next payment** – use the loan summary endpoint to see the next scheduled payment, broken down by principal/interest/fee and per borrower so everyone knows their upcoming contribution.
- **Estimate runway** – inspect the projected interest remaining (both aggregated and per borrower) to understand how future rate changes or extra payments would influence the payoff path.

These jobs are represented in the UI walkthrough below and are also achievable via the raw HTTP API.

## UX integration at a glance

1. **Upload** – the frontend lets a user pick a JSON file (for example `LoanSplitter/sample-events.json`). It sends the content to `POST /eventStream`, which deserializes the events via `UserEventJsonDeserializer` and stores the resulting `EventStream` in-memory. The response contains a stream ID.
2. **Query** – armed with the stream ID, the frontend can request either the full state (`GET /eventStream/{id}?date=…`) or the condensed summary (`GET /eventStream/{id}/loanSummary?date=…&loanName=…`).
3. **Render** – the browser fetches the JSON, formats the NOK amounts, and surfaces the totals and per-person breakdowns.

Under the hood, everything rides on a very small set of components:

- `EventStreamStore` caches streams in memory keyed by GUID.
- `EventStream` replays user events plus internally generated `MaybeEvent`s to build immutable `State` snapshots.
- `LoanSummaryResponse` projects a specific `Loan` entity into UX-friendly numbers (next payment total, per-person split, remaining principal, projected interest).
- CORS is wide open for development so the static frontend (served from any localhost port) can call the API without extra proxying.

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

### GET `/eventStream/{id}/loanSummary?date=YYYY-MM-DD&loanName=apartLoan`

Returns a computed summary for a specific loan at the requested date, including the next payment split by person plus remaining principal and projected interest totals. This endpoint powers the frontend helper described below.

**Response shape**

```json
{
    "loanName": "apartLoan",
    "snapshotDate": "2026-06-01T00:00:00+00:00",
    "remainingAmount": 6398607.23,
    "projectedInterestRemaining": 40764,
    "remainingAmountByPerson": {
        "Alin": 3199303.61,
        "Diana": 3199303.61
    },
    "projectedInterestRemainingByPerson": {
        "Alin": 20382,
        "Diana": 20382
    },
    "nextPaymentTotal": {
        "principal": 17364.23,
        "interest": 11796.69,
        "fee": 65,
        "total": 29225.92
    },
    "nextPaymentByPerson": {
        "Alin": {
            "principal": 8682.12,
            "interest": 5898.34,
            "fee": 32.5,
            "total": 14612.96
        },
        "Diana": {
            "principal": 8682.12,
            "interest": 5898.34,
            "fee": 32.5,
            "total": 14612.96
        }
    }
}
```

## Frontend playground

A lightweight static UI that calls the API lives under `LoanSplitter.Frontend/`. It lets you upload an event JSON file, pick the snapshot date, and visualize the next payment split as well as remaining principal and projected interest.

1. Start the API (enables permissive CORS by default):

```bash
dotnet run --project LoanSplitter/LoanSplitter.csproj
```

2. Serve the frontend with any static server (for example, using `npx http-server`):

```bash
npx --yes http-server LoanSplitter.Frontend -p 4173
```

3. Navigate to `http://localhost:4173`, upload your JSON events, select the loan name (auto-suggested from `LoanContracted` events), and fetch the summary for any date.

> Tip: amounts are formatted as NOK in the UI; adjust `currency` inside `LoanSplitter.Frontend/app.js` if you prefer a different currency symbol.

> Sample data: `LoanSplitter/sample-events.json` mirrors the events from `LoanSplitter.Tests/Events/EventStreamTest.cs` so you can demo the UX in seconds.

## Notes

- States are stored purely in memory; restart the service to clear all streams.
- The GET endpoint returns `404` when the date is earlier than the first event or when the stream id does not exist.
- `State.Entities` is serialized with camel-cased property names for easier client consumption.
- Use the `CorrectNextLoanPaymentSplit` event when one borrower temporarily covers more (or less) of the next installment; the override is applied once and then discarded.

## Project layout

The repository is intentionally small, but each folder plays a specific role:

- `Domain/` – immutable domain models such as `Loan`, `Account`, `AccountTransaction`, and the aggregate `State` object that represents the world at a given point in the stream.
- `Events/` – the event-sourced heart of the system, containing every `EventBase` subtype, the `EventStream` runner, and the `UserEventJsonDeserializer` that handles JSON import/export of events.
- `Services/` – currently just `EventStreamStore`, an in-memory catalog that keeps the uploaded event streams accessible via the API.
- `Program.cs` & `appsettings*.json` – the ASP.NET Core minimal API host and configuration; this wires up dependency injection and defines the `/eventStream` endpoints described above.
- `LoanSplitter.Tests/` (sibling project) – MSTest-based unit tests that exercise event logic, JSON serialization, and the event stream behavior.

Feel free to explore or extend any of these areas depending on whether you're modeling new loan behaviors, introducing additional events, or building richer API surfaces.

For a deeper look at the business entities and events the system models, see [`DOMAIN.md`](./DOMAIN.md).

