# LoanSplitter Domain Guide

This document explains the business model captured inside the `LoanSplitter` service: which entities we keep in memory, what each event represents, and how the event stream progresses from one immutable `State` to the next.

## Big picture

LoanSplitter helps model joint loans where multiple people share responsibility for a mortgage or similar long-term debt. Incoming events describe everything that can happen to that shared loan (new contracts, advance payments, interest-rate changes, etc.). Every event is applied chronologically to produce a new immutable `State` snapshot. Querying the API at a specific date is equivalent to replaying all events that occurred on or before that date and returning the latest state.

## Core entities held in `State`

| Entity | Purpose |
| --- | --- |
| `Loan` | Represents the outstanding mortgage. It tracks remaining principal, remaining term in months, annual interest rate, accrued fees/interest, pending advance payments, and optional overrides for the next payment amount. Each loan also contains per-person sub-loans that mirror the main loan so we can attribute balances per borrower. |
| `Loan.SubLoans` | A dictionary keyed by participant name (`"Alin"`, `"Diana"`, …). Every sub-loan is a lightweight `Loan` instance with its own remaining amount, term, and totals, enabling fine-grained per-person reporting. |
| `Account` | A simple ledger that stores the history of `AccountTransaction` entries (debits/credits) for a specific staging account, typically the account that funds monthly payments or receives advance payments. |
| `AccountTransaction` | A value object combining an `Amount` and `PersonName`. It explains who contributed how much for a given transaction. |
| `State` | The container we hand back to API callers. Internally it is just a dictionary `<string, object>` where the key is the entity name (for example `"apartLoan"` or `"creditAcct"`). The `State` itself is immutable; `WithUpdates` produces a new instance every time an event finishes.

## Event catalog

Every event inherits from `EventBase` and therefore carries a `Date` plus an `Apply(State)` method. Applying an event always returns an `EventOutcome` that contains (1) the dictionary of entity updates and (2) optional `MaybeEvent` instructions for the future.

| Event | What it models | Typical state updates |
| --- | --- | --- |
| `AccountCreatedEvent` | Bootstrap a staging account before any money flows. | Adds a fresh `Account` instance under the provided account name. |
| `AccountTransactionEvent` | Record a manual deposit/withdrawal that is not part of the automated loan flow. | Appends an `AccountTransaction` to the chosen `Account`. |
| `LoanContractedEvent` | Represents signing a new loan contract. | Adds the `Loan` entity, creates sub-loans for each borrower, and schedules the first `LoanPaymentEvent`. |
| `LoanPaymentEvent` | Executes a scheduled monthly payment. | Reads the current loan, splits the next payment between borrowers, withdraws the appropriate amounts from the backing account, reduces loan/sub-loan balances, accrues interest/fees, and schedules the next payment via a `MaybeEvent`. |
| `AdvancePaymentEvent` | Someone makes an extra principal payment outside the standard schedule. | Temporarily stores an `AccountTransaction` inside the `Loan` until the next payment execution, at which point principal balances are reduced accordingly. |
| `CorrectNextLoanPaymentEvent` | Overrides the next payment’s calculated principal/interest split (useful when the bank issues corrected data). | Sets a one-off `LoanPayment` override on the loan so the following `LoanPaymentEvent` will consume the corrected numbers. |
| `CorrectNextLoanPaymentSplitEvent` | Temporarily changes how the next payment is divided between participants (useful when one person fronts more of a given installment). | Stores normalized per-person contribution shares that are applied to the next `LoanPaymentEvent`, then discarded. |
| `InterestRateChangedEvent` | Bank announces a future interest rate change. | Marks the loan (and each sub-loan) with the upcoming rate so that, after the next payment, the new rate becomes active. |

> **Tip:** Events are stored and exchanged as JSON through `UserEventJsonDeserializer`. Each JSON object features a `type` discriminator plus the relevant fields listed above.

## How states evolve

1. **Start from an empty state.** When the API boots, we have an empty dictionary of entities.
2. **Order the user-provided events.** `EventStream` sorts events by date and interleaves them with automatically generated `MaybeEvent`s (such as the next scheduled payment).
3. **Apply one event at a time.** For each event we:
   - Call `Apply(State)` to produce an `EventOutcome`.
   - Merge the returned `StateUpdates` into the previous state via `state.WithUpdates(...)`, yielding a fresh immutable `State`.
   - Queue any emitted `MaybeEvent` for future execution (for example, the monthly payment scheduler).
4. **Historical lookup.** The stream keeps every intermediate `(date, state)` tuple so that the GET endpoint can return the correct snapshot for any requested date.

The process is purely functional: no event mutates an existing `State`. Instead, each one produces a new instance, ensuring historical timelines remain reproducible and easy to reason about.

## When to introduce new events or entities

- **New financial products or behaviors:** add a new `EventBase` subclass plus supporting domain logic (new methods on `Loan`, `Account`, or new domain types).
- **Different contribution rules:** extend `Loan.GetSubLoanShares()` or the `AccountTransaction` model if you support more people or weighted splits.
- **Additional read models:** because the `State` is just a dictionary, you can store any object graph you need, as long as events know how to update it.

Keeping this document up to date when you add new events or entities will help API consumers (and future you) understand how a stream of events becomes the state reported by `/eventStream/{id}?date=...`.
