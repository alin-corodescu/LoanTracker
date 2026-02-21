import { useState, useEffect } from 'react';

const EVENT_TYPES = [
  { value: 'LoanContracted', label: 'Loan Contracted' },
  { value: 'LoanPayment', label: 'Loan Payment' },
  { value: 'InterestRateChanged', label: 'Interest Rate Changed' },
  { value: 'AdvancePayment', label: 'Advance Payment' },
  { value: 'BillCreated', label: 'Bill Created' },
];

function EventEditor({ onSwitchToViewer }) {
  const [events, setEvents] = useState(() => {
    const stored = localStorage.getItem('eventStreamEditor');
    try {
      return stored ? JSON.parse(stored) : [];
    } catch {
      return [];
    }
  });
  
  const [editingIndex, setEditingIndex] = useState(null);
  const [showForm, setShowForm] = useState(false);
  const [importError, setImportError] = useState('');

  useEffect(() => {
    localStorage.setItem('eventStreamEditor', JSON.stringify(events));
  }, [events]);

  const handleAddEvent = (event) => {
    if (editingIndex !== null) {
      const updated = [...events];
      updated[editingIndex] = event;
      setEvents(updated);
      setEditingIndex(null);
    } else {
      setEvents([...events, event]);
    }
    setShowForm(false);
  };

  const handleDeleteEvent = (index) => {
    setEvents(events.filter((_, i) => i !== index));
  };

  const handleEditEvent = (index) => {
    setEditingIndex(index);
    setShowForm(true);
  };

  const handleCancel = () => {
    setShowForm(false);
    setEditingIndex(null);
  };

  const handleCreateStream = () => {
    onSwitchToViewer(events);
  };

  const handleFileImport = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setImportError('');
    
    try {
      const fileText = await file.text();
      const importedEvents = JSON.parse(fileText);
      
      if (!Array.isArray(importedEvents)) {
        throw new Error('JSON file must contain an array of events');
      }
      
      setEvents(importedEvents);
      e.target.value = ''; // Reset file input
    } catch (error) {
      setImportError(`Failed to import: ${error.message}`);
      e.target.value = ''; // Reset file input
    }
  };

  const sortedEvents = [...events].sort((a, b) => new Date(a.date) - new Date(b.date));

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h2 className="text-xl font-semibold tracking-wide">Event Editor</h2>
        <div className="flex gap-2">
          {!showForm && (
            <>
              <label className="px-4 py-2 rounded-lg font-semibold text-sm bg-slate-700 text-slate-200 hover:bg-slate-600 cursor-pointer">
                📁 Load from File
                <input
                  type="file"
                  accept="application/json"
                  onChange={handleFileImport}
                  className="hidden"
                />
              </label>
              <button
                onClick={() => setShowForm(true)}
                className="px-4 py-2 rounded-lg font-semibold text-sm bg-blue-600 text-white hover:bg-blue-700"
              >
                + Add Event
              </button>
            </>
          )}
          {events.length > 0 && (
            <button
              onClick={handleCreateStream}
              className="px-4 py-2 rounded-lg font-semibold text-sm bg-gradient-to-r from-blue-600 to-purple-600 text-white hover:from-blue-700 hover:to-purple-700"
            >
              Create Stream ({events.length} event{events.length !== 1 ? 's' : ''})
            </button>
          )}
        </div>
      </div>

      {importError && (
        <div className="bg-red-900/20 border border-red-700/50 rounded-lg p-3 text-sm text-red-300">
          {importError}
        </div>
      )}

      {showForm && (
        <EventForm
          initialEvent={editingIndex !== null ? events[editingIndex] : null}
          onSave={handleAddEvent}
          onCancel={handleCancel}
        />
      )}

      {sortedEvents.length === 0 && !showForm && (
        <div className="text-center py-12 text-slate-400">
          <p>No events yet. Click "Add Event" to get started.</p>
        </div>
      )}

      {sortedEvents.length > 0 && (
        <div className="space-y-2">
          {sortedEvents.map((event, idx) => {
            const originalIndex = events.findIndex(e => e === event);
            return (
              <EventCard
                key={originalIndex}
                event={event}
                onEdit={() => handleEditEvent(originalIndex)}
                onDelete={() => handleDeleteEvent(originalIndex)}
              />
            );
          })}
        </div>
      )}
    </div>
  );
}

function EventCard({ event, onEdit, onDelete }) {
  const formatDate = (dateStr) => {
    const date = new Date(dateStr);
    return date.toLocaleDateString();
  };

  const getEventSummary = (event) => {
    switch (event.type) {
      case 'LoanContracted':
        return `${event.loanName}: ${event.principal} @ ${event.nominalRate}% for ${event.term} months`;
      case 'LoanPayment':
        return `${event.loanName} from ${event.fromAccountName}`;
      case 'InterestRateChanged':
        return `${event.loanName} → ${event.rate}%`;
      case 'AdvancePayment':
        return `${event.loanName}: ${event.transaction?.person} pays ${event.transaction?.amount}`;
      case 'BillCreated':
        return `${event.billName}: ${event.description}`;
      default:
        return JSON.stringify(event);
    }
  };

  return (
    <div className="bg-slate-800/50 border border-slate-700/30 rounded-xl p-4 flex justify-between items-start hover:bg-slate-700/50 transition-all">
      <div className="flex-1">
        <div className="flex items-center gap-3 mb-1">
          <span className="text-sm font-semibold text-cyan-400">{event.type}</span>
          <span className="text-sm text-slate-400">{formatDate(event.date)}</span>
        </div>
        <p className="text-sm text-slate-300">{getEventSummary(event)}</p>
      </div>
      <div className="flex gap-2 ml-4">
        <button
          onClick={onEdit}
          className="px-3 py-1 text-sm rounded-lg bg-slate-700 hover:bg-slate-600 text-slate-200"
        >
          Edit
        </button>
        <button
          onClick={onDelete}
          className="px-3 py-1 text-sm rounded-lg bg-red-900/50 hover:bg-red-900 text-red-200"
        >
          Delete
        </button>
      </div>
    </div>
  );
}

function EventForm({ initialEvent, onSave, onCancel }) {
  const [eventType, setEventType] = useState(initialEvent?.type || 'LoanContracted');
  const [formData, setFormData] = useState(initialEvent || {
    type: 'LoanContracted',
    date: new Date().toISOString().split('T')[0],
  });

  useEffect(() => {
    if (initialEvent) {
      setEventType(initialEvent.type);
      setFormData(initialEvent);
    }
  }, [initialEvent]);

  const handleTypeChange = (newType) => {
    setEventType(newType);
    setFormData({
      type: newType,
      date: formData.date,
    });
  };

  const handleChange = (field, value) => {
    setFormData({ ...formData, [field]: value });
  };

  const handleNestedChange = (parent, field, value) => {
    setFormData({
      ...formData,
      [parent]: { ...formData[parent], [field]: value }
    });
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    onSave(formData);
  };

  const renderFields = () => {
    switch (eventType) {
      case 'LoanContracted':
        return (
          <>
            <InputField label="Loan Name" value={formData.loanName || ''} onChange={(v) => handleChange('loanName', v)} required />
            <InputField label="Principal" type="number" value={formData.principal || ''} onChange={(v) => handleChange('principal', parseFloat(v))} required />
            <InputField label="Nominal Rate (%)" type="number" step="0.01" value={formData.nominalRate || ''} onChange={(v) => handleChange('nominalRate', parseFloat(v))} required />
            <InputField label="Term (months)" type="number" value={formData.term || ''} onChange={(v) => handleChange('term', parseInt(v))} required />
            <InputField label="Backing Account Name" value={formData.backingAccountName || ''} onChange={(v) => handleChange('backingAccountName', v)} required />
            <InputField label="Person 1 Name" value={formData.name1 || ''} onChange={(v) => handleChange('name1', v)} required />
            <InputField label="Person 2 Name" value={formData.name2 || ''} onChange={(v) => handleChange('name2', v)} required />
          </>
        );
      case 'LoanPayment':
        return (
          <>
            <InputField label="Loan Name" value={formData.loanName || ''} onChange={(v) => handleChange('loanName', v)} required />
            <InputField label="From Account Name" value={formData.fromAccountName || ''} onChange={(v) => handleChange('fromAccountName', v)} required />
          </>
        );
      case 'InterestRateChanged':
        return (
          <>
            <InputField label="Loan Name" value={formData.loanName || ''} onChange={(v) => handleChange('loanName', v)} required />
            <InputField label="New Rate (%)" type="number" step="0.01" value={formData.rate || ''} onChange={(v) => handleChange('rate', parseFloat(v))} required />
          </>
        );
      case 'AdvancePayment':
        return (
          <>
            <InputField label="Loan Name" value={formData.loanName || ''} onChange={(v) => handleChange('loanName', v)} required />
            <div className="border border-slate-600/50 rounded-lg p-4 space-y-3">
              <h4 className="text-sm font-semibold text-slate-300">Transaction</h4>
              <InputField label="Person" value={formData.transaction?.person || ''} onChange={(v) => handleNestedChange('transaction', 'person', v)} required />
              <InputField label="Amount" type="number" value={formData.transaction?.amount || ''} onChange={(v) => handleNestedChange('transaction', 'amount', parseFloat(v))} required />
              <InputField label="Category" value={formData.transaction?.category || ''} onChange={(v) => handleNestedChange('transaction', 'category', v)} required />
            </div>
          </>
        );
      case 'BillCreated':
        return (
          <>
            <InputField label="Bill Name" value={formData.billName || ''} onChange={(v) => handleChange('billName', v)} required />
            <InputField label="Description" value={formData.description || ''} onChange={(v) => handleChange('description', v)} required />
            <InputField label="Account Name" value={formData.accountName || ''} onChange={(v) => handleChange('accountName', v)} required />
            <div className="border border-slate-600/50 rounded-lg p-4">
              <h4 className="text-sm font-semibold text-slate-300 mb-3">Items (JSON array)</h4>
              <textarea
                value={JSON.stringify(formData.items || [], null, 2)}
                onChange={(e) => {
                  try {
                    handleChange('items', JSON.parse(e.target.value));
                  } catch {
                    // Ignore parse errors while typing
                  }
                }}
                className="w-full h-32 rounded-lg border border-slate-600/50 bg-slate-900/60 px-3 py-2 text-sm font-mono"
                placeholder='[{"amount": 100, "person": "Alice", "category": "Food"}]'
              />
            </div>
          </>
        );
      default:
        return null;
    }
  };

  return (
    <form onSubmit={handleSubmit} className="bg-slate-800/50 border border-slate-700/50 rounded-xl p-6 space-y-4">
      <div>
        <label className="block text-sm font-semibold mb-2">Event Type</label>
        <select
          value={eventType}
          onChange={(e) => handleTypeChange(e.target.value)}
          className="w-full rounded-lg border border-slate-600/50 bg-slate-900/60 px-4 py-2 text-base"
        >
          {EVENT_TYPES.map(type => (
            <option key={type.value} value={type.value}>{type.label}</option>
          ))}
        </select>
      </div>

      <InputField
        label="Date"
        type="date"
        value={formData.date || ''}
        onChange={(v) => handleChange('date', v)}
        required
      />

      {renderFields()}

      <div className="flex gap-3 pt-2">
        <button
          type="submit"
          className="flex-1 bg-blue-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-blue-700"
        >
          {initialEvent ? 'Update Event' : 'Add Event'}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="flex-1 bg-slate-700 text-slate-200 font-semibold py-2 px-4 rounded-lg hover:bg-slate-600"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}

function InputField({ label, type = 'text', value, onChange, required = false, step, placeholder }) {
  return (
    <div>
      <label className="block text-sm font-semibold mb-2">{label}</label>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
        step={step}
        placeholder={placeholder}
        className="w-full rounded-lg border border-slate-600/50 bg-slate-900/60 px-4 py-2 text-base"
      />
    </div>
  );
}

export default EventEditor;
