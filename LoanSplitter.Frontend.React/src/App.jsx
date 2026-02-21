import { useState } from 'react';
import StateSnapshot from './StateSnapshot';
import EventEditor from './EventEditor';

const API_BASE_URL = 'http://localhost:5000';

function App() {
  const [currentView, setCurrentView] = useState('editor');
  const [uploadStatus, setUploadStatus] = useState({ message: '', type: '' });
  const [currentStreamId, setCurrentStreamId] = useState(null);

  const handleSwitchToViewer = async (events) => {
    const eventsJson = JSON.stringify(events);
    
    try {
      const response = await fetch(`${API_BASE_URL}/eventStream`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: eventsJson,
      });

      const payload = await response.json();

      if (!response.ok) {
        throw new Error(payload?.error ?? 'Upload failed.');
      }

      setCurrentStreamId(payload.id);
      setUploadStatus({ message: 'Event stream created successfully!', type: 'success' });
      
      // Auto-redirect to State Snapshot view
      setCurrentView('stateSnapshot');
    } catch (error) {
      setUploadStatus({ message: error.message ?? 'An unexpected error occurred.', type: 'error' });
      setCurrentStreamId(null);
    }
  };

  if (currentView === 'stateSnapshot') {
    return (
      <div className="max-w-4xl mx-auto px-6 py-12">
        <header className="mb-8">
          <h1 className="text-4xl font-bold mb-2">LoanSplitter playground</h1>
          <div className="mt-4 flex gap-2">
            <button
              onClick={() => setCurrentView('editor')}
              className="px-4 py-2 rounded-lg font-semibold transition-all bg-slate-800 text-slate-300 hover:bg-slate-700"
            >
              Event Editor
            </button>
            <button
              onClick={() => setCurrentView('stateSnapshot')}
              className="px-4 py-2 rounded-lg font-semibold transition-all bg-blue-600 text-white"
            >
              State Snapshot
            </button>
          </div>
        </header>
        <StateSnapshot 
          initialStreamId={currentStreamId} 
          onStreamIdChange={setCurrentStreamId}
        />
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-6 py-12 space-y-6">
      <header className="mb-8">
        <h1 className="text-4xl font-bold mb-2">LoanSplitter playground</h1>
        <p className="text-slate-300">
          Create and manage event streams for loan tracking.
        </p>
        <div className="mt-4 flex gap-2">
          <button
            onClick={() => setCurrentView('editor')}
            className="px-4 py-2 rounded-lg font-semibold transition-all bg-blue-600 text-white"
          >
            Event Editor
          </button>
          <button
            onClick={() => setCurrentView('stateSnapshot')}
            className="px-4 py-2 rounded-lg font-semibold transition-all bg-slate-800 text-slate-300 hover:bg-slate-700"
          >
            State Snapshot
          </button>
        </div>
      </header>

      <section className="bg-slate-900/80 border border-slate-700/50 rounded-2xl p-6 shadow-2xl">
        <EventEditor onSwitchToViewer={handleSwitchToViewer} />
        
        {uploadStatus.message && (
          <p className={`mt-4 text-sm ${
            uploadStatus.type === 'success' ? 'text-emerald-400' :
            uploadStatus.type === 'error' ? 'text-red-400' : 'text-amber-400'
          }`}>
            {uploadStatus.message}
          </p>
        )}
      </section>
    </div>
  );
}

export default App;
