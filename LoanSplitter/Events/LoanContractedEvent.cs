using LoanSplitter.Domain;

namespace LoanSplitter.Events;

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
    public override EventOutcome Apply(State state)
    {
        var loan = new Loan(principal, nominalRate, term, name1, name2);
        var updates = new Dictionary<string, object>
        {
            { loanName, loan }
        };

        return new EventOutcome(updates,
            LoanPaymentEvent.CreateNextPaymentMaybeEvent(Date,
                backingAccountName,
                loanName,
                term));
    }
}