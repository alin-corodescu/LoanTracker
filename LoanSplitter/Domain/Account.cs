namespace LoanSplitter.Domain;

public class Account
{
    private List<AccountTransaction> _accountTransactions = new List<AccountTransaction>();
    
    public Account WithTransaction(AccountTransaction payment)
    {
        var updatedAccount = new Account();
        
        updatedAccount._accountTransactions = new List<AccountTransaction>(this._accountTransactions);
        
        updatedAccount._accountTransactions.Add(payment);
        
        return updatedAccount;
    }
}