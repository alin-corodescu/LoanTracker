namespace LoanSplitter.Domain;

public class Account
{
    private List<AccountTransaction> _accountTransactions = new();

    public Account WithTransaction(AccountTransaction payment)
    {
        return new Account
        {
            _accountTransactions =
            [
                .._accountTransactions,
                payment
            ]
        };
        ;
    }
}