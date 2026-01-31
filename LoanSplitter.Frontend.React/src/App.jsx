import { useState } from 'react';
import StateSnapshot from './StateSnapshot';

const API_BASE_URL = 'http://localhost:5000';

const currencyFormatter = new Intl.NumberFormat(undefined, {
  style: 'currency',
  currency: 'NOK',
  maximumFractionDigits: 2,
});

function App() {
  const [currentView, setCurrentView] = useState('loanSummary');
  const [currentStreamId, setCurrentStreamId] = useState(null);
  const [loanNames, setLoanNames] = useState([]);
  const [loanName, setLoanName] = useState('');
  const [snapshotDate, setSnapshotDate] = useState(new Date().toISOString().split('T')[0]);
  const [uploadStatus, setUploadStatus] = useState({ message: '', type: '' });
  const [summaryStatus, setSummaryStatus] = useState({ message: '', type: '' });
  const [summary, setSummary] = useState(null);
  const [recentEvents, setRecentEvents] = useState([]);

  if (currentView === 'stateSnapshot') {
    return <StateSnapshot />;
  }

  const extractLoanNames = (fileText) => {
    try {
      const events = JSON.parse(fileText);
      if (!Array.isArray(events)) return [];
      
      const names = new Set();
      for (const evt of events) {
        const type = evt?.type?.toString().toLowerCase();
        if (type === 'loancontracted' && typeof evt.loanName === 'string') {
          names.add(evt.loanName);
        }
      }
      return [...names];
    } catch {
      return [];
    }
  };

  const handleFileUpload = async (e) => {
    e.preventDefault();
    const fileInput = e.target.elements.file;
    const file = fileInput.files?.[0];
    
    setSummary(null);
    setSummaryStatus({ message: '', type: '' });
    setRecentEvents([]);

    if (!file) {
      setUploadStatus({ message: 'Please choose a JSON file before uploading.', type: 'error' });
      return;
    }

    const fileText = await file.text();
    const extractedNames = extractLoanNames(fileText);
    setLoanNames(extractedNames);

    try {
      const response = await fetch(`${API_BASE_URL}/eventStream`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: fileText,
      });

      const payload = await response.json();

      if (!response.ok) {
        throw new Error(payload?.error ?? 'Upload failed.');
      }

      setCurrentStreamId(payload.id);
      setUploadStatus({ message: 'Event stream created successfully!', type: 'success' });

      if (!loanName && extractedNames.length === 1) {
        setLoanName(extractedNames[0]);
      }
    } catch (error) {
      setUploadStatus({ message: error.message ?? 'An unexpected error occurred.', type: 'error' });
      setCurrentStreamId(null);
    }
  };

  const fetchRecentEvents = async (date) => {
    if (!currentStreamId) return;

    try {
      const url = new URL(`${API_BASE_URL}/eventStream/${currentStreamId}/events`);
      url.searchParams.set('date', date);

      const response = await fetch(url);
      const events = await response.json();

      if (!response.ok) return;

      const recent = events.slice(-5).reverse();
      setRecentEvents(recent);
    } catch (error) {
      console.error('Failed to fetch events:', error);
    }
  };

  const handleFetchSummary = async () => {
    if (!currentStreamId) {
      setSummaryStatus({ message: 'Upload an event file first to create a stream.', type: 'error' });
      return;
    }

    if (!loanName.trim()) {
      setSummaryStatus({ message: 'Loan name is required (for example, apartLoan).', type: 'error' });
      return;
    }

    if (!snapshotDate) {
      setSummaryStatus({ message: 'Please select a snapshot date.', type: 'error' });
      return;
    }

    setSummaryStatus({ message: 'Fetching summary...', type: 'progress' });

    try {
      const url = new URL(`${API_BASE_URL}/eventStream/${currentStreamId}/loanSummary`);
      url.searchParams.set('date', snapshotDate);
      url.searchParams.set('loanName', loanName);

      const response = await fetch(url);
      const payload = await response.json();

      if (!response.ok) {
        throw new Error(payload?.error ?? 'Failed to load summary.');
      }

      setSummary(payload);
      await fetchRecentEvents(snapshotDate);
      setSummaryStatus({ message: '', type: '' });
    } catch (error) {
      setSummary(null);
      setRecentEvents([]);
      setSummaryStatus({ message: error.message ?? 'Failed to load summary.', type: 'error' });
    }
  };

  const formatCurrency = (value) => {
    if (typeof value !== 'number' || Number.isNaN(value)) return '-';
    return currencyFormatter.format(value);
  };

  const formatDate = (dateStr) => {
    if (!dateStr) return snapshotDate;
    const date = new Date(dateStr);
    if (Number.isNaN(date.getTime())) return dateStr;
    return date.toLocaleDateString();
  };

  const handleEventClick = (date) => {
    setSnapshotDate(date);
    setTimeout(() => handleFetchSummary(), 0);
  };

  return (
    <div className="max-w-4xl mx-auto px-6 py-12 space-y-6">
      <header className="mb-8">
        <h1 className="text-4xl font-bold mb-2">LoanSplitter playground</h1>
        <p className="text-slate-300">
          Upload your event JSON, pick a date, and inspect the next payment plus projected interest for each borrower.
        </p>
        <div className="mt-4 flex gap-2">
          <button
            onClick={() => setCurrentView('loanSummary')}
            className={`px-4 py-2 rounded-lg font-semibold transition-all ${
              currentView === 'loanSummary'
                ? 'bg-blue-600 text-white'
                : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
            }`}
          >
            Loan Summary
          </button>
          <button
            onClick={() => setCurrentView('stateSnapshot')}
            className={`px-4 py-2 rounded-lg font-semibold transition-all ${
              currentView === 'stateSnapshot'
                ? 'bg-blue-600 text-white'
                : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
            }`}
          >
            State Snapshot
          </button>
        </div>
      </header>

      <section className="bg-slate-900/80 border border-slate-700/50 rounded-2xl p-6 shadow-2xl">
        <h2 className="text-xl font-semibold mb-4 tracking-wide">1. Upload events</h2>
        <form onSubmit={handleFileUpload} className="space-y-4">
          <div>
            <label className="block text-sm font-semibold mb-2">Event JSON file</label>
            <input
              name="file"
              type="file"
              accept="application/json"
              required
              className="w-full rounded-xl border border-slate-600/50 bg-slate-900/60 px-4 py-3 text-base transition-colors focus:outline-none focus:border-cyan-400"
            />
          </div>
          <button
            type="submit"
            className="w-full bg-gradient-to-r from-blue-600 to-purple-600 text-white font-semibold py-3 px-4 rounded-xl hover:from-blue-700 hover:to-purple-700 transition-all active:translate-y-px"
          >
            Create event stream
          </button>
        </form>
        {uploadStatus.message && (
          <p className={`mt-3 text-sm ${
            uploadStatus.type === 'success' ? 'text-emerald-400' :
            uploadStatus.type === 'error' ? 'text-red-400' : 'text-amber-400'
          }`}>
            {uploadStatus.message}
          </p>
        )}
        {currentStreamId && (
          <p className="mt-3 text-sm font-mono text-emerald-400 break-all">
            Stream ID: {currentStreamId}
          </p>
        )}
      </section>

      <section className="bg-slate-900/80 border border-slate-700/50 rounded-2xl p-6 shadow-2xl">
        <h2 className="text-xl font-semibold mb-4 tracking-wide">2. Pick loan + snapshot date</h2>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-semibold mb-2">Loan name</label>
            <input
              type="text"
              list="loan-names"
              placeholder="e.g. apartLoan"
              value={loanName}
              onChange={(e) => setLoanName(e.target.value)}
              className="w-full rounded-xl border border-slate-600/50 bg-slate-900/60 px-4 py-3 text-base transition-colors focus:outline-none focus:border-cyan-400"
            />
            <datalist id="loan-names">
              {loanNames.map(name => <option key={name} value={name} />)}
            </datalist>
          </div>
          <div>
            <label className="block text-sm font-semibold mb-2">Snapshot date</label>
            <input
              type="date"
              value={snapshotDate}
              onChange={(e) => setSnapshotDate(e.target.value)}
              className="w-full rounded-xl border border-slate-600/50 bg-slate-900/60 px-4 py-3 text-base transition-colors focus:outline-none focus:border-cyan-400"
            />
          </div>
          <button
            onClick={handleFetchSummary}
            className="w-full bg-gradient-to-r from-blue-600 to-purple-600 text-white font-semibold py-3 px-4 rounded-xl hover:from-blue-700 hover:to-purple-700 transition-all active:translate-y-px"
          >
            Get loan summary
          </button>
          {summaryStatus.message && (
            <p className={`text-sm ${
              summaryStatus.type === 'success' ? 'text-emerald-400' :
              summaryStatus.type === 'error' ? 'text-red-400' :
              summaryStatus.type === 'progress' ? 'text-blue-300' : 'text-amber-400'
            }`}>
              {summaryStatus.message}
            </p>
          )}
        </div>
      </section>

      {recentEvents.length > 0 && (
        <section className="bg-slate-900/80 border border-slate-700/50 rounded-2xl p-6 shadow-2xl">
          <h2 className="text-xl font-semibold mb-2 tracking-wide">Recent events</h2>
          <p className="text-sm text-slate-400 mb-4">
            Last 5 events before the snapshot date. Click an event to jump to its date.
          </p>
          <ul className="space-y-2">
            {recentEvents.map((evt, idx) => {
              const dateStr = new Date(evt.date).toISOString().split('T')[0];
              return (
                <li
                  key={idx}
                  onClick={() => handleEventClick(dateStr)}
                  className="bg-slate-800/50 border border-slate-700/30 rounded-xl px-4 py-3 cursor-pointer hover:bg-slate-700/80 hover:border-cyan-400 transition-all flex justify-between items-center"
                >
                  <span className="text-sm text-slate-100">{evt.type}</span>
                  <span className="text-sm font-mono font-semibold text-cyan-400">{dateStr}</span>
                </li>
              );
            })}
          </ul>
        </section>
      )}

      <section className="bg-slate-900/80 border border-slate-700/50 rounded-2xl p-6 shadow-2xl min-h-[200px]">
        <h2 className="text-xl font-semibold mb-4 tracking-wide">3. Loan summary</h2>
        {summary ? (
          <div className="space-y-6">
            <p className="text-slate-300">
              Summary for <strong>{summary.loanName}</strong> at <strong>{formatDate(summary.snapshotDate)}</strong>
            </p>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <article className="border border-slate-700/40 rounded-2xl p-4 bg-slate-900/60">
                <h3 className="text-base font-medium text-blue-300 mb-1">Remaining principal</h3>
                <p className="text-2xl font-semibold mb-2">{formatCurrency(summary.remainingAmount)}</p>
                {Object.entries(summary.remainingAmountByPerson ?? {}).length > 0 && (
                  <ul className="space-y-1 text-sm text-slate-300">
                    {Object.entries(summary.remainingAmountByPerson).map(([person, amount]) => (
                      <li key={person} className="flex justify-between">
                        <span>{person}</span>
                        <span>{formatCurrency(amount)}</span>
                      </li>
                    ))}
                  </ul>
                )}
              </article>

              <article className="border border-slate-700/40 rounded-2xl p-4 bg-slate-900/60">
                <h3 className="text-base font-medium text-blue-300 mb-1">Projected interest remaining</h3>
                <p className="text-2xl font-semibold mb-2">{formatCurrency(summary.projectedInterestRemaining)}</p>
                {Object.entries(summary.projectedInterestRemainingByPerson ?? {}).length > 0 && (
                  <ul className="space-y-1 text-sm text-slate-300">
                    {Object.entries(summary.projectedInterestRemainingByPerson).map(([person, amount]) => (
                      <li key={person} className="flex justify-between">
                        <span>{person}</span>
                        <span>{formatCurrency(amount)}</span>
                      </li>
                    ))}
                  </ul>
                )}
              </article>
            </div>

            <div className="overflow-x-auto">
              <h3 className="text-lg font-semibold mb-3">Next payment</h3>
              {Object.entries(summary.nextPaymentByPerson ?? {}).length > 0 ? (
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-slate-700">
                      <th className="text-left py-2 px-2 text-sm uppercase tracking-wider text-slate-400">Person</th>
                      <th className="text-left py-2 px-2 text-sm uppercase tracking-wider text-slate-400">Principal</th>
                      <th className="text-left py-2 px-2 text-sm uppercase tracking-wider text-slate-400">Interest</th>
                      <th className="text-left py-2 px-2 text-sm uppercase tracking-wider text-slate-400">Fee</th>
                      <th className="text-left py-2 px-2 text-sm uppercase tracking-wider text-slate-400">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    {Object.entries(summary.nextPaymentByPerson).map(([person, payment]) => (
                      <tr key={person} className="odd:bg-slate-900/50">
                        <td className="py-2 px-2">{person}</td>
                        <td className="py-2 px-2">{formatCurrency(payment.principal)}</td>
                        <td className="py-2 px-2">{formatCurrency(payment.interest)}</td>
                        <td className="py-2 px-2">{formatCurrency(payment.fee)}</td>
                        <td className="py-2 px-2">{formatCurrency(payment.total ?? payment.principal + payment.interest + payment.fee)}</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr className="border-t border-slate-700 font-semibold">
                      <td className="py-3 px-2">Total</td>
                      <td className="py-3 px-2">{formatCurrency(summary.nextPaymentTotal?.principal)}</td>
                      <td className="py-3 px-2">{formatCurrency(summary.nextPaymentTotal?.interest)}</td>
                      <td className="py-3 px-2">{formatCurrency(summary.nextPaymentTotal?.fee)}</td>
                      <td className="py-3 px-2">{formatCurrency(summary.nextPaymentTotal?.total)}</td>
                    </tr>
                  </tfoot>
                </table>
              ) : (
                <p className="text-slate-400">No borrower split available for this loan.</p>
              )}
            </div>
          </div>
        ) : null}
      </section>
    </div>
  );
}

export default App;
