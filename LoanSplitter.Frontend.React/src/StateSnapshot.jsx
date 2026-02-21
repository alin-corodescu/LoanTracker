import { useState } from 'react';

const API_BASE_URL = 'http://localhost:5000';

const currencyFormatter = new Intl.NumberFormat(undefined, {
  style: 'currency',
  currency: 'NOK',
  maximumFractionDigits: 2,
});

function StateSnapshot({ initialStreamId, onStreamIdChange }) {
  const [currentStreamId, setCurrentStreamId] = useState(initialStreamId);
  const [cutoffDate, setCutoffDate] = useState(new Date().toISOString().split('T')[0]);
  const [uploadStatus, setUploadStatus] = useState({ message: '', type: '' });
  const [snapshotStatus, setSnapshotStatus] = useState({ message: '', type: '' });
  const [stateSnapshot, setStateSnapshot] = useState(null);

  const updateStreamId = (id) => {
    setCurrentStreamId(id);
    onStreamIdChange?.(id);
  };

  const handleFileUpload = async (e) => {
    e.preventDefault();
    const fileInput = e.target.elements.file;
    const file = fileInput.files?.[0];
    
    setStateSnapshot(null);
    setSnapshotStatus({ message: '', type: '' });

    if (!file) {
      setUploadStatus({ message: 'Please choose a JSON file before uploading.', type: 'error' });
      return;
    }

    const fileText = await file.text();

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

      updateStreamId(payload.id);
      setUploadStatus({ message: 'Event stream created successfully!', type: 'success' });
    } catch (error) {
      setUploadStatus({ message: error.message ?? 'An unexpected error occurred.', type: 'error' });
      updateStreamId(null);
    }
  };

  const handleFetchSnapshot = async () => {
    if (!currentStreamId) {
      setSnapshotStatus({ message: 'Upload an event file first to create a stream.', type: 'error' });
      return;
    }

    if (!cutoffDate) {
      setSnapshotStatus({ message: 'Please select a cutoff date.', type: 'error' });
      return;
    }

    setSnapshotStatus({ message: 'Fetching state snapshot...', type: 'progress' });

    try {
      const response = await fetch(`${API_BASE_URL}/eventStream/${currentStreamId}/stateSnapshot`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ cutoffDate }),
      });

      const payload = await response.json();

      if (!response.ok) {
        throw new Error(payload?.error ?? 'Failed to load state snapshot.');
      }

      setStateSnapshot(payload);
      setSnapshotStatus({ message: '', type: '' });
    } catch (error) {
      setStateSnapshot(null);
      setSnapshotStatus({ message: error.message ?? 'Failed to load state snapshot.', type: 'error' });
    }
  };

  const formatCurrency = (value) => {
    if (typeof value !== 'number' || Number.isNaN(value)) return '-';
    return currencyFormatter.format(value);
  };

  const formatDate = (dateStr) => {
    if (!dateStr) return cutoffDate;
    const date = new Date(dateStr);
    if (Number.isNaN(date.getTime())) return dateStr;
    return date.toLocaleDateString();
  };

  return (
    <div className="space-y-6">
      {currentStreamId && (
        <div className="bg-emerald-900/20 border border-emerald-700/50 rounded-xl p-4">
          <p className="text-sm font-mono text-emerald-300">
            Active Stream ID: {currentStreamId}
          </p>
        </div>
      )}

      <section className="bg-slate-900/80 border border-slate-700/50 rounded-2xl p-6 shadow-2xl">
        <h2 className="text-xl font-semibold mb-4 tracking-wide">
          {currentStreamId ? 'Or upload a different event stream' : '1. Upload events'}
        </h2>
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
        <h2 className="text-xl font-semibold mb-4 tracking-wide">
          {currentStreamId ? 'Pick cutoff date' : '2. Pick cutoff date'}
        </h2>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-semibold mb-2">Cutoff date</label>
            <input
              type="date"
              value={cutoffDate}
              onChange={(e) => setCutoffDate(e.target.value)}
              className="w-full rounded-xl border border-slate-600/50 bg-slate-900/60 px-4 py-3 text-base transition-colors focus:outline-none focus:border-cyan-400"
            />
          </div>
          <button
            onClick={handleFetchSnapshot}
            className="w-full bg-gradient-to-r from-blue-600 to-purple-600 text-white font-semibold py-3 px-4 rounded-xl hover:from-blue-700 hover:to-purple-700 transition-all active:translate-y-px"
          >
            Get state snapshot
          </button>
          {snapshotStatus.message && (
            <p className={`text-sm ${
              snapshotStatus.type === 'success' ? 'text-emerald-400' :
              snapshotStatus.type === 'error' ? 'text-red-400' :
              snapshotStatus.type === 'progress' ? 'text-blue-300' : 'text-amber-400'
            }`}>
              {snapshotStatus.message}
            </p>
          )}
        </div>
      </section>

      {stateSnapshot && (
        <section className="bg-slate-900/80 border border-slate-700/50 rounded-2xl p-6 shadow-2xl">
          <h2 className="text-xl font-semibold mb-4 tracking-wide">
            State at {formatDate(stateSnapshot.snapshotDate)}
          </h2>
          
          <div className="space-y-6">
            {/* Loans Section */}
            {Object.keys(stateSnapshot.loans || {}).length > 0 && (
              <div>
                <h3 className="text-lg font-semibold mb-3 text-blue-300">Loans</h3>
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                  {Object.entries(stateSnapshot.loans).map(([name, loan]) => (
                    <article key={name} className="border border-slate-700/40 rounded-xl p-4 bg-slate-900/60">
                      <h4 className="text-base font-semibold mb-2 text-slate-100">{name}</h4>
                      <div className="space-y-1 text-sm text-slate-300">
                        <div className="flex justify-between">
                          <span>Remaining amount:</span>
                          <span className="font-semibold">{formatCurrency(loan.remainingAmount)}</span>
                        </div>
                        <div className="flex justify-between">
                          <span>Remaining term:</span>
                          <span className="font-semibold">{loan.remainingTermInMonths} months</span>
                        </div>
                        {loan.subLoans && Object.keys(loan.subLoans).length > 0 && (
                          <div className="mt-2 pt-2 border-t border-slate-700/50">
                            <span className="text-xs uppercase tracking-wider text-slate-400">Sub-loans:</span>
                            {Object.entries(loan.subLoans).map(([person, subLoan]) => (
                              <div key={person} className="flex justify-between mt-1">
                                <span>{person}:</span>
                                <span className="font-semibold">{formatCurrency(subLoan.remainingAmount)}</span>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    </article>
                  ))}
                </div>
              </div>
            )}

            {/* Accounts Section */}
            {Object.keys(stateSnapshot.accounts || {}).length > 0 && (
              <div>
                <h3 className="text-lg font-semibold mb-3 text-green-300">Accounts</h3>
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                  {Object.entries(stateSnapshot.accounts).map(([name, account]) => (
                    <article key={name} className="border border-slate-700/40 rounded-xl p-4 bg-slate-900/60">
                      <h4 className="text-base font-semibold mb-2 text-slate-100">{name}</h4>
                      <div className="space-y-1 text-sm text-slate-300">
                        <div className="flex justify-between">
                          <span>Transactions:</span>
                          <span className="font-semibold">{account.transactions?.length || 0}</span>
                        </div>
                        {account.transactions && account.transactions.length > 0 && (
                          <div className="mt-2 pt-2 border-t border-slate-700/50">
                            <span className="text-xs uppercase tracking-wider text-slate-400">Recent:</span>
                            <div className="max-h-32 overflow-y-auto space-y-1 mt-1">
                              {account.transactions.slice(-5).reverse().map((txn, idx) => (
                                <div key={idx} className="flex justify-between text-xs">
                                  <span>{txn.personName || 'N/A'}</span>
                                  <span className="font-mono">{formatCurrency(txn.amount)}</span>
                                </div>
                              ))}
                            </div>
                          </div>
                        )}
                      </div>
                    </article>
                  ))}
                </div>
              </div>
            )}

            {/* Bills Section */}
            {Object.keys(stateSnapshot.bills || {}).length > 0 && (
              <div>
                <h3 className="text-lg font-semibold mb-3 text-purple-300">Bills</h3>
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                  {Object.entries(stateSnapshot.bills).map(([name, bill]) => (
                    <article key={name} className="border border-slate-700/40 rounded-xl p-4 bg-slate-900/60">
                      <h4 className="text-base font-semibold mb-2 text-slate-100">{name}</h4>
                      <div className="space-y-1 text-sm text-slate-300">
                        <div className="flex justify-between">
                          <span>Description:</span>
                          <span className="font-semibold">{bill.description}</span>
                        </div>
                        <div className="flex justify-between">
                          <span>Date:</span>
                          <span className="font-semibold">{formatDate(bill.date)}</span>
                        </div>
                        <div className="flex justify-between">
                          <span>Total amount:</span>
                          <span className="font-semibold">{formatCurrency(bill.totalAmount)}</span>
                        </div>
                        {bill.items && bill.items.length > 0 && (
                          <div className="mt-2 pt-2 border-t border-slate-700/50">
                            <span className="text-xs uppercase tracking-wider text-slate-400">Items:</span>
                            <div className="space-y-1 mt-1">
                              {bill.items.map((item, idx) => (
                                <div key={idx} className="flex justify-between text-xs">
                                  <span>{item.personName} ({item.category})</span>
                                  <span className="font-mono">{formatCurrency(item.amount)}</span>
                                </div>
                              ))}
                            </div>
                          </div>
                        )}
                      </div>
                    </article>
                  ))}
                </div>
              </div>
            )}

            {/* Empty state */}
            {Object.keys(stateSnapshot.loans || {}).length === 0 &&
             Object.keys(stateSnapshot.accounts || {}).length === 0 &&
             Object.keys(stateSnapshot.bills || {}).length === 0 && (
              <p className="text-slate-400 text-center py-8">
                No entities found in the state at this date.
              </p>
            )}
          </div>
        </section>
      )}
    </div>
  );
}

export default StateSnapshot;
