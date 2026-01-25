using LoanSplitter.Domain;

namespace LoanSplitter.Api;

/// <summary>
/// Projects a specific Loan entity into UX-friendly JSON for /eventStream/{id}/loanSummary.
/// Provides computed summaries including:
/// - Next payment total and per-person split
/// - Remaining principal (aggregated and per borrower)
/// - Projected interest remaining (aggregated and per borrower)
/// 
/// Must remain in sync with frontend expectations in LoanSplitter.Frontend/app.js.
/// </summary>
public sealed class LoanSummaryResponse
{
    public string LoanName { get; init; } = string.Empty;

    public LoanPaymentDto NextPaymentTotal { get; init; } = LoanPaymentDto.Empty;

    public Dictionary<string, LoanPaymentDto> NextPaymentByPerson { get; init; } = new();

    public double RemainingAmount { get; init; }

    public Dictionary<string, double> RemainingAmountByPerson { get; init; } = new();

    public double ProjectedInterestRemaining { get; init; }

    public Dictionary<string, double> ProjectedInterestRemainingByPerson { get; init; } = new();

    public DateTime SnapshotDate { get; init; }

    public static LoanSummaryResponse From(string loanName, Loan loan, DateTime snapshotDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loanName);
        ArgumentNullException.ThrowIfNull(loan);

        var nextPaymentTotal = LoanPaymentDto.From(loan.GetNextMonthlyPayment());

        var nextPaymentByPerson = loan.SubLoans.Any()
            ? loan.GetNextMonthlySplitPayment()
                .ToDictionary(kvp => kvp.Key, kvp => LoanPaymentDto.From(kvp.Value))
            : new Dictionary<string, LoanPaymentDto>();

        var remainingAmountByPerson = loan.SubLoans
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.RemainingAmount);

        var projectedInterestByPerson = loan.SubLoans
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetProjectedInterestRemaining());

        return new LoanSummaryResponse
        {
            LoanName = loanName,
            NextPaymentTotal = nextPaymentTotal,
            NextPaymentByPerson = nextPaymentByPerson,
            RemainingAmount = loan.RemainingAmount,
            RemainingAmountByPerson = remainingAmountByPerson,
            ProjectedInterestRemaining = loan.GetProjectedInterestRemaining(),
            ProjectedInterestRemainingByPerson = projectedInterestByPerson,
            SnapshotDate = snapshotDate
        };
    }
}

public sealed record LoanPaymentDto(double Principal, double Interest, double Fee)
{
    public double Total => Principal + Interest + Fee;

    public static LoanPaymentDto Empty { get; } = new(0, 0, 0);

    public static LoanPaymentDto From(Loan.LoanPayment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
        return new LoanPaymentDto(payment.Principal, payment.Interest, payment.Fee);
    }
}
