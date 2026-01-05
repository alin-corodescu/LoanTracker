# LoanSplitter Frontend Playground

A single-page static experience that helps you upload event timelines, query the backend, and visualize the next loan payment per borrower.

## Jobs this UX supports

1. **Import** – load a JSON file that lists all historical events (e.g., `sample-events.json`). The UI posts it straight to `POST /eventStream` and surfaces the generated stream ID.
2. **Inspect** – choose a loan name and date to pull the immutable state summary (`GET /eventStream/{id}/loanSummary`). The page highlights remaining principal, projected interest, and the next payment split by person.
3. **Plan** – tweak the snapshot date to see how projected interest and the next payment evolve over time, helping borrowers decide when to add advance payments or expect higher interest charges.

## Tech design

- **Plain HTML/CSS/JS:** no frameworks; the page lives in `index.html`, `styles.css`, and `app.js`.
- **Fetch-first workflow:**
  - Upload form reads the selected file, infers loan names from `LoanContracted` events, and `fetch`es the API with `application/json`.
  - Summary button builds a query to `/eventStream/{id}/loanSummary?date=YYYY-MM-DD&loanName=...` and renders the response.
- **CORS friendly:** the backend enables a permissive policy (`FrontendDevPolicy`) so any localhost origin can talk to it during development.
- **Formatting helpers:** `Intl.NumberFormat` formats NOK currency, and datalists suggest loan names extracted from the uploaded file.

## Run locally

1. Start the API (enables CORS automatically):

```bash
cd ..
dotnet run --project LoanSplitter/LoanSplitter.csproj
```

2. Serve the static files from this folder. Any dev server works; here is an example:

```bash
npx --yes http-server . -p 4173
```

3. Visit `http://localhost:4173`, upload your JSON file, provide the loan name if needed, pick a date, and hit **Get loan summary**.

## Customization tips

- **Currency:** change the `currency` option inside `app.js` if you prefer USD/EUR/etc.
- **Endpoints:** adjust `API_BASE_URL` if the backend runs on another host or port.
- **Styling:** tweak `styles.css` for brand colors or typography; the layout is a simple CSS grid/stack structure.

## Troubleshooting

- If uploads fail immediately, check the browser console for JSON validation errors (missing `type`, malformed dates, etc.).
- A `404` from the summary endpoint usually means the stream ID expired (server restarted) or the loan name doesn’t exist in that stream.
- Remember that all state is in-memory; restart the API and re-upload events to reset.
