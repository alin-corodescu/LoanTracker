# LoanTracker AI Guide

## Backend
- **Tech stack:** ASP.NET Core minimal API, C# 12
- **Key folders:**
  - `LoanSplitter/Domain/` - domain models (Loan, Account, Bill, State)
  - `LoanSplitter/Events/` - event sourcing logic (EventBase subclasses, EventStream, deserializers)
  - `LoanSplitter/Api/` - read models and DTOs
  - `LoanSplitter/Services/` - infrastructure (EventStreamStore)
  - `LoanSplitter/Program.cs` - API endpoint definitions and DI setup
  - `LoanSplitter.Tests/` - MSTest unit tests
- **Documentation:** Implementation details, patterns, and conventions are documented directly in the C# code files via XML doc comments. Read the code.

## Frontend
- **Tech stack:** Vanilla HTML/CSS/JavaScript (no framework or bundler)
- **Key folders:**
  - `LoanSplitter.Frontend/` - static files (index.html, styles.css, app.js)

## Documentation philosophy
- **High-level architecture and user flows:** Keep these in markdown files (`architecture.md`, `user-journeys.md`)
- **Implementation details:** Document in the appropriate code files themselves via comments, XML doc comments, or class/method summaries
- When making changes, update markdown files only if architecture or user flows change; update code comments for implementation details
