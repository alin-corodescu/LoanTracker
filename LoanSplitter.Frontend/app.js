const API_BASE_URL = "http://localhost:5000";

const uploadForm = document.getElementById("upload-form");
const fileInput = document.getElementById("event-file");
const uploadStatus = document.getElementById("upload-status");
const streamIdDisplay = document.getElementById("stream-id");
const loanNameInput = document.getElementById("loan-name");
const loanNameOptions = document.getElementById("loan-name-options");
const dateInput = document.getElementById("state-date");
const fetchSummaryButton = document.getElementById("fetch-summary");
const summaryStatus = document.getElementById("summary-status");
const summaryOutput = document.getElementById("summary-output");
const eventsSection = document.getElementById("events-section");
const eventsList = document.getElementById("events-list");

let currentStreamId = null;

const currencyFormatter = new Intl.NumberFormat(undefined, {
    style: "currency",
    currency: "NOK",
    maximumFractionDigits: 2,
});

const numberFormatter = new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
});

const today = new Date().toISOString().split("T")[0];
dateInput.value = today;

uploadForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    summaryOutput.innerHTML = "";
    summaryStatus.textContent = "";
    eventsSection.style.display = "none";

    const file = fileInput.files?.[0];
    if (!file) {
        setStatus(uploadStatus, "Please choose a JSON file before uploading.");
        return;
    }

    const fileText = await file.text();
    const loanNames = extractLoanNames(fileText);
    updateLoanSuggestions(loanNames);

    try {
        const response = await fetch(`${API_BASE_URL}/eventStream`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: fileText,
        });

        const payload = await response.json();

        if (!response.ok) {
            throw new Error(payload?.error ?? "Upload failed.");
        }

        currentStreamId = payload.id;
        setStatus(uploadStatus, "Event stream created successfully!", "success");
        streamIdDisplay.textContent = `Stream ID: ${currentStreamId}`;

        if (!loanNameInput.value && loanNames.length === 1) {
            loanNameInput.value = loanNames[0];
        }
    } catch (error) {
        setStatus(uploadStatus, error.message ?? "An unexpected error occurred.", "error");
        currentStreamId = null;
        streamIdDisplay.textContent = "";
    }
});

fetchSummaryButton.addEventListener("click", async () => {
    if (!currentStreamId) {
        setStatus(summaryStatus, "Upload an event file first to create a stream.", "error");
        return;
    }

    const loanName = loanNameInput.value.trim();
    const snapshotDate = dateInput.value;

    if (!loanName) {
        setStatus(summaryStatus, "Loan name is required (for example, apartLoan).", "error");
        return;
    }

    if (!snapshotDate) {
        setStatus(summaryStatus, "Please select a snapshot date.", "error");
        return;
    }

    setStatus(summaryStatus, "Fetching summary...", "progress");

    try {
        const url = new URL(`${API_BASE_URL}/eventStream/${currentStreamId}/loanSummary`);
        url.searchParams.set("date", snapshotDate);
        url.searchParams.set("loanName", loanName);

        const response = await fetch(url);
        const payload = await response.json();

        if (!response.ok) {
            throw new Error(payload?.error ?? "Failed to load summary.");
        }

        renderSummary(payload);
        await fetchRecentEvents(snapshotDate);
        setStatus(summaryStatus, "");
    } catch (error) {
        summaryOutput.innerHTML = "";
        eventsSection.style.display = "none";
        setStatus(summaryStatus, error.message ?? "Failed to load summary.", "error");
    }
});

async function fetchRecentEvents(snapshotDate) {
    if (!currentStreamId) return;

    try {
        const url = new URL(`${API_BASE_URL}/eventStream/${currentStreamId}/events`);
        url.searchParams.set("date", snapshotDate);

        const response = await fetch(url);
        const events = await response.json();

        if (!response.ok) return;

        // Take the last 5 events
        const recentEvents = events.slice(-5).reverse();
        renderEvents(recentEvents);
    } catch (error) {
        console.error("Failed to fetch events:", error);
    }
}

function renderEvents(events) {
    if (!events.length) {
        eventsSection.style.display = "none";
        return;
    }

    eventsSection.style.display = "block";
    eventsList.innerHTML = events.map(evt => {
        const dateStr = new Date(evt.date).toISOString().split("T")[0];
        return `
            <li class="event-item" data-date="${dateStr}">
                <span class="event-type">${evt.type}</span>
                <span class="event-date">${dateStr}</span>
            </li>
        `;
    }).join("");

    // Add click listeners
    eventsList.querySelectorAll(".event-item").forEach(item => {
        item.addEventListener("click", () => {
            const date = item.getAttribute("data-date");
            dateInput.value = date;
            fetchSummaryButton.click();
        });
    });
}

function renderSummary(summary) {
    const nextPaymentRows = Object.entries(summary.nextPaymentByPerson ?? {}).map(([person, payment]) => {
        return `<tr>
            <td>${person}</td>
            <td>${formatCurrency(payment.principal)}</td>
            <td>${formatCurrency(payment.interest)}</td>
            <td>${formatCurrency(payment.fee)}</td>
            <td>${formatCurrency(payment.total ?? payment.principal + payment.interest + payment.fee)}</td>
        </tr>`;
    }).join("");

    const remainingList = buildPersonList(summary.remainingAmountByPerson);
    const projectedList = buildPersonList(summary.projectedInterestRemainingByPerson);

    summaryOutput.innerHTML = `
        <p class="summary-intro">Summary for <strong>${summary.loanName}</strong> at <strong>${summaryDate(summary)}</strong></p>
        <div class="summary-grid">
            <article>
                <h3>Remaining principal</h3>
                <p>${formatCurrency(summary.remainingAmount)}</p>
                ${remainingList}
            </article>
            <article>
                <h3>Projected interest remaining</h3>
                <p>${formatCurrency(summary.projectedInterestRemaining)}</p>
                ${projectedList}
            </article>
        </div>
        <div class="table-wrapper">
            <h3>Next payment</h3>
            ${nextPaymentRows ? `
            <table>
                <thead>
                    <tr>
                        <th>Person</th>
                        <th>Principal</th>
                        <th>Interest</th>
                        <th>Fee</th>
                        <th>Total</th>
                    </tr>
                </thead>
                <tbody>
                    ${nextPaymentRows}
                </tbody>
                <tfoot>
                    <tr>
                        <td>Total</td>
                        <td>${formatCurrency(summary.nextPaymentTotal?.principal)}</td>
                        <td>${formatCurrency(summary.nextPaymentTotal?.interest)}</td>
                        <td>${formatCurrency(summary.nextPaymentTotal?.fee)}</td>
                        <td>${formatCurrency(summary.nextPaymentTotal?.total)}</td>
                    </tr>
                </tfoot>
            </table>` : `<p>No borrower split available for this loan.</p>`}
        </div>
    `;
}

function summaryDate(summary) {
    if (!summary.snapshotDate) return dateInput.value;
    const date = new Date(summary.snapshotDate);
    if (Number.isNaN(date.getTime())) return summary.snapshotDate;
    return date.toLocaleDateString();
}

function buildPersonList(map) {
    const entries = Object.entries(map ?? {});
    if (!entries.length) return "";

    const items = entries.map(([person, value]) => `<li><span>${person}</span><span>${formatCurrency(value)}</span></li>`).join("");
    return `<ul class="person-list">${items}</ul>`;
}

function formatCurrency(value) {
    if (typeof value !== "number" || Number.isNaN(value)) return "-";
    return currencyFormatter.format(value);
}

function extractLoanNames(rawText) {
    try {
        const events = JSON.parse(rawText);
        if (!Array.isArray(events)) return [];

        const names = new Set();
        for (const evt of events) {
            const type = evt?.type?.toString().toLowerCase();
            if (type === "loancontracted" && typeof evt.loanName === "string") {
                names.add(evt.loanName);
            }
        }
        return [...names];
    } catch {
        return [];
    }
}

function updateLoanSuggestions(names) {
    loanNameOptions.innerHTML = names.map((name) => `<option value="${name}"></option>`).join("");
}

function setStatus(element, message, tone = "info") {
    element.textContent = message;
    element.classList.remove("success", "error", "progress");
    if (message) {
        element.classList.add(tone);
    }
}
