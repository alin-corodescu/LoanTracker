using LoanSplitter.Domain;

namespace LoanSplitter.Events;

/// <summary>
/// Someone makes an extra principal payment outside the standard schedule.
/// Temporarily stores an AccountTransaction inside the Loan until the next payment execution, at which point principal balances are reduced accordingly.
/// Also records a bill in state to track the advance payment.
/// </summary>
public class AdvancePaymentEvent(DateTime date, string loanName, AccountTransaction transaction) : EventBase(date)
{
    public string LoanName { get; } = loanName;
    public AccountTransaction Transaction { get; } = transaction;

    public override EventOutcome Apply(State state)
    {
        var loan = state.GetEntityByName<Loan>(LoanName);
        var updatedLoan = loan.WithAdvancePayment(Transaction);

        var billName = $"{LoanName}_advance_{Date:yyyy-MM-dd}_{Transaction.PersonName}";
        var bill = new Bill(
            $"Advance payment for {LoanName}",
            new List<BillItem> { new(Transaction.Amount, Transaction.PersonName, "AdvancePayment") },
            Transaction.PersonName,
            new Dictionary<string, double> { { Transaction.PersonName, 1.0 } }
        );

        var updates = new Dictionary<string, object>(BillCreatedEvent.ComputeStateUpdates(state, billName, bill))
        {
            { LoanName, updatedLoan }
        };

        return new EventOutcome(updates);
    }
}