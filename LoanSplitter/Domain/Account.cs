using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LoanSplitter.Domain;

public class Account
{
    private List<AccountTransaction> _accountTransactions = new();

    public IReadOnlyList<AccountTransaction> Transactions => new ReadOnlyCollection<AccountTransaction>(_accountTransactions);

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
    }
}