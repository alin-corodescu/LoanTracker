using LoanSplitter.Domain;

namespace LoanSplitter.Events;

/// <summary>
/// Represents signing a new loan contract.
/// Adds the Loan entity, creates sub-loans for each borrower, and schedules the first LoanPaymentEvent.
/// </summary>
public class LoanContractedEvent(
    DateTime date,
    string loanName,
    double principal,
    double nominalRate,
    int term,
    string backingAccountName,
    string name1,
    string name2)
    : EventBase(date)
{
    public string LoanName { get; } = loanName;
    public double Principal { get; } = principal;
    public double NominalRate { get; } = nominalRate;
    public int Term { get; } = term;
    public string BackingAccountName { get; } = backingAccountName;
    public string Name1 { get; } = name1;
    public string Name2 { get; } = name2;

    public override EventOutcome Apply(State state)
    {
        var loan = new Loan(Principal, NominalRate, Term, Name1, Name2);
        var updates = new Dictionary<string, object>
        {
            { LoanName, loan }
        };

        return new EventOutcome(updates,
            LoanPaymentEvent.CreateNextPaymentMaybeEvent(Date,
                BackingAccountName,
                LoanName,
                Term));
    }
}