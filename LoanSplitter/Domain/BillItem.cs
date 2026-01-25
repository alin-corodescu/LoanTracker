namespace LoanSplitter.Domain;

public class BillItem
{
    public BillItem(double amount, string personName, string category)
    {
        Amount = amount;
        PersonName = personName ?? throw new ArgumentNullException(nameof(personName));
        Category = category ?? throw new ArgumentNullException(nameof(category));
    }

    public double Amount { get; }
    public string PersonName { get; }
    public string Category { get; }
}
