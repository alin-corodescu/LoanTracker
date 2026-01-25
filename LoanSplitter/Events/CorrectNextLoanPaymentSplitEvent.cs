using System.Collections.Generic;
using System.Collections.ObjectModel;
using LoanSplitter.Domain;

namespace LoanSplitter.Events;

/// <summary>
/// Temporarily changes how the next payment is divided between participants (useful when one person fronts more of a given installment).
/// Stores normalized per-person contribution shares that are applied to the next LoanPaymentEvent, then discarded.
/// </summary>
public sealed class CorrectNextLoanPaymentSplitEvent : EventBase
{
    public CorrectNextLoanPaymentSplitEvent(
        DateTime date,
        string loanName,
        IReadOnlyDictionary<string, double> contributions)
        : base(date)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loanName);
        ArgumentNullException.ThrowIfNull(contributions);
        if (contributions.Count == 0)
            throw new ArgumentException("At least one contribution override is required.", nameof(contributions));

        LoanName = loanName;
        Contributions = new ReadOnlyDictionary<string, double>(new Dictionary<string, double>(contributions));
    }

    public string LoanName { get; }

    public IReadOnlyDictionary<string, double> Contributions { get; }

    public override EventOutcome Apply(State state)
    {
        var loan = state.GetEntityByName<Loan>(LoanName);
        var updatedLoan = loan.WithCorrectNextPaymentSplit(Contributions);

        return new EventOutcome(new Dictionary<string, object> { { LoanName, updatedLoan } });
    }
}
