using LoanSplitter.Domain;

namespace LoanSplitter.Events;

public class AccountTransactionEvent(DateTime date, string acctName, AccountTransaction transaction)
    : EventBase(date)
{
    public override EventOutcome Apply(State state)
    {
        var acct = state.GetEntityByName<Account>(acctName);
        
        var updatedAcct = acct.WithTransaction(transaction);
        
        return new EventOutcome(new Dictionary<string, object> { { acctName, updatedAcct } });
    }
}