using LoanSplitter.Domain;

namespace LoanSplitter.Events;

/// <summary>
/// Executes a scheduled monthly payment.
/// Reads the current loan, splits the next payment between borrowers, creates a Bill with items for each participant (category "LoanPayment"),
/// applies the bill to update the account via bill.ApplyToAccount(), reduces loan/sub-loan balances, accrues interest/fees,
/// and schedules the next payment via a MaybeEvent.
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

        // Create the bill
        var billName = $"{LoanName}_payment_{Date:yyyy-MM-dd}";
        var bill = new Bill($"Loan payment for {LoanName}", billItems, Date);

        // Apply the bill to update the account
        var account = state.GetEntityByName<Account>(FromAccountName);
        var updatedAccount = bill.ApplyToAccount(account);

        // Execute the advance payments and interest changes for the next month
        var updatedLoan = loan.WithExecuteNextPayment();

        var updates = new Dictionary<string, object>
        {
            { FromAccountName, updatedAccount },
            { LoanName, updatedLoan },
            { billName, bill }
        };

        var remainingTerm = updatedLoan.RemainingTermInMonths;

        return new EventOutcome(updates, CreateNextPaymentMaybeEvent(Date, FromAccountName, LoanName, remainingTerm));
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