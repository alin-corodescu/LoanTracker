using System.Collections.Generic;
using LoanSplitter.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LoanSplitter.Tests.Domain;

[TestClass]
public class LoanTests
{
    [TestMethod]
    public void ProjectedInterestRemainingMatchesAmortizationFormula()
    {
        var remainingAmount = 500_000d;
        const double annualRate = 4.5;
        const int remainingTerm = 240;

        var loan = new Loan(remainingAmount, annualRate, remainingTerm, "Alex", "Jamie");

        var projectedInterest = loan.GetProjectedInterestRemaining();

        var monthlyRate = annualRate / 12 / 100;
        var denominator = 1 - Math.Pow(1 + monthlyRate, -remainingTerm);
        var monthlyPayment = remainingAmount * monthlyRate / denominator;
        var expectedInterest = monthlyPayment * remainingTerm - remainingAmount;

        Assert.AreEqual(expectedInterest, projectedInterest, 0.01, "Projected interest should match the amortization formula.");
    }

    [TestMethod]
    public void ProjectedInterestRemainingIsZeroWhenRateIsZero()
    {
        var loan = new Loan(250_000, 0, 180, "Alex", "Jamie");

        Assert.AreEqual(0, loan.GetProjectedInterestRemaining(), 0.001);
    }

    [TestMethod]
    public void CorrectNextPaymentSplitOverridesShareOnce()
    {
        var loan = new Loan(1_000_000, 5, 240, "Alex", "Jamie");

        var overrides = new Dictionary<string, double>
        {
            { "Alex", 3 },
            { "Jamie", 1 }
        };

        var adjusted = loan.WithCorrectNextPaymentSplit(overrides);

        var split = adjusted.GetNextMonthlySplitPayment();

        var alexPrincipal = split["Alex"].Principal;
        var jamiePrincipal = split["Jamie"].Principal;

        Assert.AreEqual(3d, alexPrincipal / jamiePrincipal, 0.05, "Override should adjust principal ratio to 3:1.");

        var afterPayment = adjusted.WithExecuteNextPayment();
        var nextSplit = afterPayment.GetNextMonthlySplitPayment();
        var totalPrincipal = nextSplit.Sum(kvp => kvp.Value.Principal);

        foreach (var person in afterPayment.SubLoans.Keys)
        {
            var expectedShare = afterPayment.SubLoans[person].RemainingAmount / afterPayment.RemainingAmount;
            var actualShare = nextSplit[person].Principal / totalPrincipal;
            Assert.AreEqual(expectedShare, actualShare, 0.001, "Override should only affect the next payment.");
        }
    }

    [TestMethod]
    public void CorrectNextPaymentSplitValidatesParticipants()
    {
        var loan = new Loan(500_000, 4.5, 180, "Alex", "Jamie");

        try
        {
            loan.WithCorrectNextPaymentSplit(new Dictionary<string, double>
            {
                { "Casey", 1 }
            });

            Assert.Fail("Expected invalid participant to throw.");
        }
        catch (InvalidOperationException)
        {
            // expected
        }
    }
}
