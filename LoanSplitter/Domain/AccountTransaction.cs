namespace LoanSplitter.Domain;

public class AccountTransaction
{
    public AccountTransaction(double amount, string personName)
    {
        Amount = amount;
        PersonName = personName;
    }

    public string PersonName { get; }

    public double Amount { get; }
}