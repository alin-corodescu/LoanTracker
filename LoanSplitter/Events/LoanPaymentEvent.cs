using LoanSplitter.Domain;

namespace LoanSplitter.Events;

public class LoanPaymentEvent(DateTime date, string fromAccountName, string loanName)
    : EventBase(date)
{
    public override EventOutcome Apply(State state)
    {
        // Payment from the staging account to the loan.
        // the loan is also an account. But how do we keep track of whose money is it in the account?
        
        // Begin transaction, Modify elements. Commit transaction.
        var loan = state.GetEntityByName<Loan>(loanName);
        
        // The monthly amount, split by person.
        var payments = loan.GetNextMonthlySplitPayment();
        
        // Reduce the amount in the fromAccount with X for Alin, Y for Diana.
        var updatedAccount = state.GetEntityByName<Account>(fromAccountName);

        foreach (var payment in payments)
        {
            var transaction = new AccountTransaction(
                amount: payment.Value.Principal + payment.Value.Interest + payment.Value.Fee,
                personName: payment.Key);
            
            updatedAccount = updatedAccount.WithTransaction(transaction);
        }

        // At this point, we need to internally execute the advance payments and interest changes for the next month.
        var updatedLoan = loan.WithExecuteNextPayment();

        var updates = new Dictionary<string, object>()
        {
            { fromAccountName, updatedAccount },
            { loanName, updatedLoan }
        };

        int remainingTerm = updatedLoan.RemainingTermInMonths;
        
        // Next month from the date of the event.
        // End of the month
        
        var firstOfThisMonth = new DateTime(this.Date.Year, this.Date.Month, 1);
        var lastDayOfNextMonth = firstOfThisMonth.AddMonths(2).AddDays(-1);
        
        return new EventOutcome(updates, CreateNextPaymentMaybeEvent(this.Date, fromAccountName, loanName, remainingTerm));
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
            {
                return new LoanPaymentEvent(lastDayOfNextMonth, fromAccountName, loanName);
            }

            return null;
        });
    }
}