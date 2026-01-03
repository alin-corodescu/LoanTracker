using LoanSplitter.Domain;

namespace LoanSplitter.Events;

public class CorrectNextLoanPaymentEvent(DateTime date, string loanName, double principal, double interest)
    : EventBase(date)
{
    public string LoanName { get; } = loanName;
    public double Principal { get; } = principal;
    public double Interest { get; } = interest;

    public override EventOutcome Apply(State state)
    {
        var loan = state.GetEntityByName<Loan>(LoanName);

        var updatedLoan = loan.WithCorrectNextPayment(Principal, Interest);

        return new EventOutcome(new Dictionary<string, object> { { LoanName, updatedLoan } });
    }
}