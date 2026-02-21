using LoanSplitter.Domain;

namespace LoanSplitter.Events;

/// <summary>
/// Executes a scheduled monthly payment.
/// Reads the current loan, splits the next payment between borrowers, creates a BillCreatedEvent to track the payment,
/// reduces loan/sub-loan balances, accrues interest/fees, and schedules the next payment via a MaybeEvent.
/// The BillCreatedEvent is processed inline to update the account and create the bill entity.
/// </summary>
public class LoanPaymentEvent(DateTime date, string fromAccountName, string loanName)
    : EventBase(date)
{
    public string FromAccountName { get; } = fromAccountName;
    public string LoanName { get; } = loanName;

    public override EventOutcome Apply(State state)
    {
        var loan = state.GetEntityByName<Loan>(LoanName);
        var payments = loan.GetNextMonthlySplitPayment();

        // Create bill items for each person's payment
        var billItems = payments.Select(payment =>
        {
            var amount = payment.Value.Principal + payment.Value.Interest + payment.Value.Fee;
            return new BillItem(amount, payment.Key, "LoanPayment");
        }).ToList();

        // Create the bill with share information
        var billName = $"{LoanName}_payment_{Date:yyyy-MM-dd}";
        var shares = payments.ToDictionary(
            p => p.Key,
            p => (double)((p.Value.Principal + p.Value.Interest + p.Value.Fee) / billItems.Sum(bi => bi.Amount))
        );
        var bill = new Bill($"Loan payment for {LoanName}", billItems, FromAccountName, shares);

        // Execute the advance payments and interest changes for the next month
        var updatedLoan = loan.WithExecuteNextPayment();

        var updates = new Dictionary<string, object>
        {
            { LoanName, updatedLoan }
        };

        var remainingTerm = updatedLoan.RemainingTermInMonths;

        // Create the BillCreatedEvent to be processed inline
        var billCreatedEvent = new BillCreatedEvent(Date, billName, FromAccountName, bill);

        return new EventOutcome(
            updates, 
            CreateNextPaymentMaybeEvent(Date, FromAccountName, LoanName, remainingTerm),
            new List<EventBase> { billCreatedEvent }
        );
    }

    public static MaybeEvent CreateNextPaymentMaybeEvent(DateTime eventTime, string fromAccountName, string loanName,
        int remainingTermInMonths)
    {
        var firstOfThisMonth = new DateTime(eventTime.Year, eventTime.Month, 1);
        var lastDayOfNextMonth = firstOfThisMonth.AddMonths(2).AddDays(-1);

        return new MaybeEvent(lastDayOfNextMonth, s =>
        {
            var loanThen = s.GetEntityByName<Loan>(loanName);

            if (loanThen.RemainingTermInMonths == remainingTermInMonths && loanThen.RemainingTermInMonths >= 0)
                return new LoanPaymentEvent(lastDayOfNextMonth, fromAccountName, loanName);

            return null;
        });
    }
}