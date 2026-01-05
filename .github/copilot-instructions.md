# LoanTracker AI Guide

## Architecture snapshot
- The repo has three siblings: `LoanSplitter/` (ASP.NET Core minimal API), `LoanSplitter.Frontend/` (static HTML/JS playground), and `LoanSplitter.Tests/` (MSTest suite). Start backend work inside `LoanSplitter`, frontend tweaks inside `LoanSplitter.Frontend`.
- `EventStreamStore` (`Services/`) holds uploaded streams purely in memory keyed by GUID; there is no persistence, so avoid features that assume durable storage.
- Each stream replays user events plus scheduled `MaybeEvent`s (`Events/`) into immutable `State` snapshots. `Loan` in `Domain/` is intentionally functional: methods like `WithExecuteNextPayment`, `WithAdvancePayment`, and `WithInterestRate` always clone before mutating. Follow that pattern when adding new behaviors.
- Read models live in DTOs under `Api/` (currently just `LoanSummaryResponse`). They project domain objects into UX-friendly JSON for `/eventStream/{id}/loanSummary`.

## Key workflows
- Run the API with `dotnet run --project LoanSplitter/LoanSplitter.csproj`; CORS policy `FrontendDevPolicy` already allows localhost frontends.
- Serve the playground via `npx --yes http-server LoanSplitter.Frontend -p 4173` (or any static server) and point the browser to `http://localhost:4173`.
- Tests use MSTest: `dotnet test LoanSplitter.Tests/LoanSplitter.Tests.csproj`. `EventStreamEndpointsTests` exercise the HTTP layer; `Domain/` and `Events/` tests cover replay logic.
- Sample data lives at `LoanSplitter/sample-events.json` and mirrors the fixture in `LoanSplitter.Tests/Events/EventStreamTest.cs`—use it for manual smoke tests.

## Conventions & patterns
- **Events over commands:** every workflow is encoded as an `EventBase` subclass with an `Apply(State)` method returning an `EventOutcome`. If you introduce new behavior, prefer adding an event plus domain helpers rather than mutating state directly.
- **JSON contract:** `UserEventJsonDeserializer` is the single source of truth for the event schema. Each JSON object must have a `type` discriminator and `date`. When adding event fields, update both the deserializer switch and any serializer logic.
- **State updates:** `State` is a dictionary keyed by entity name (`Entities[string]`). Event handlers should return concrete domain objects (e.g., `Loan`, `Account`) and let `State.WithUpdates` replace entries.
- **Loan math:** `Loan.GetNextMonthlyPayment()` and `Loan.GetNextMonthlySplitPayment()` compute amortization using the current annual rate and term. Keep fee handling (`_monthlyFee = 65 NOK`) and sub-loan share calculations consistent if you extend the model.
- **Summary projection:** `LoanSummaryResponse.From` must remain in sync with frontend expectations in `LoanSplitter.Frontend/app.js` (fields like `nextPaymentByPerson`, `remainingAmountByPerson`). If you add properties, render them in the UI or document why they are server-only.

## Frontend pointers
- Pure HTML/CSS/JS (see `index.html`, `styles.css`, `app.js`). No bundler or framework—write vanilla modules and keep `API_BASE_URL` accurate.
- Upload flow parses the selected JSON to infer loan names before hitting the API. Maintain that UX when touching file-handling logic.
- Currency formatting uses `Intl.NumberFormat` with `currency: "NOK"`; change both server comments and UI helpers if you ever change currency semantics.

## Helpful references
- `LoanSplitter/DOMAIN.md` documents every event type and state entity—consult it before making behavioral changes.
- `LoanSplitter/Events/LoanPaymentEvent.cs` shows how scheduled payments withdraw from accounts, reduce loan balances, and queue the next payment via `MaybeEvent.CreateNextPaymentMaybeEvent`.
- `LoanSplitter/Program.cs` wires DI, JSON settings (camelCase, case-insensitive), and the three endpoints you can extend.
- Keep README files (`LoanSplitter/README.md` and `LoanSplitter.Frontend/README.md`) in sync with major workflow changes; they double as onboarding docs for humans.
