using LoanSplitter.Domain;
using LoanSplitter.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LoanSplitter.Tests.Events;

[TestClass]
public class LoanPaymentEventTests
{
    [TestMethod]
    public void LoanPaymentEvent_ShouldCreateBill()
    {
        // Arrange
        const string acctName = "creditAcct";
        const string loanName = "testLoan";

        var events = new List<EventBase>
        {
            new LoanContractedEvent(
                new DateTime(2025, 11, 1),
                loanName,
                1000000,
                4.5,
                360,
                acctName,
                "Alin",
                "Diana")
        };

        var stream = new EventStream(events);
        
        // Get state after loan payment
        var stateAfterPayment = stream.GetStateForDate(new DateTime(2025, 12, 31));

        // Assert
        Assert.IsNotNull(stateAfterPayment);

        // Verify that a bill was created (payment happens at end of next month after contract)
        var billName = $"{loanName}_payment_2025-12-31";
        var bill = stateAfterPayment!.GetEntityByName<Bill>(billName);
        
        Assert.IsNotNull(bill);
        Assert.AreEqual($"Loan payment for {loanName}", bill.Description);
        Assert.AreEqual(2, bill.Items.Count);
        
        // Verify bill items have the correct category
        foreach (var item in bill.Items)
        {
            Assert.AreEqual("LoanPayment", item.Category);
            Assert.IsTrue(item.Amount > 0);
            Assert.IsTrue(item.PersonName == "Alin" || item.PersonName == "Diana");
        }

        // Verify total bill amount matches loan payment
        var loan = stateAfterPayment.GetEntityByName<Loan>(loanName);
        var monthlyPayment = loan.GetNextMonthlyPayment();
        var expectedTotal = monthlyPayment.Principal + monthlyPayment.Interest + monthlyPayment.Fee;
        
        // Total bill should be approximately the monthly payment (allowing for rounding)
        Assert.IsTrue(Math.Abs(bill.TotalAmount - expectedTotal) < 1.0);
    }

    [TestMethod]
    public void LoanPaymentEvent_MultipleBillsCreatedOverTime()
    {
        // Arrange
        const string acctName = "creditAcct";
        const string loanName = "testLoan";

        var events = new List<EventBase>
        {
            new LoanContractedEvent(
                new DateTime(2025, 11, 1),
                loanName,
                1000000,
                4.5,
                360,
                acctName,
                "Alin",
                "Diana")
        };

        var stream = new EventStream(events);
        
        // Get state after multiple payments (need to go beyond 3 payments)
        var stateAfterThreeMonths = stream.GetStateForDate(new DateTime(2026, 3, 1));

        // Assert
        Assert.IsNotNull(stateAfterThreeMonths);

        // Verify that multiple bills were created
        var bill1Name = $"{loanName}_payment_2025-12-31";
        var bill2Name = $"{loanName}_payment_2026-01-31";
        var bill3Name = $"{loanName}_payment_2026-02-28";

        var bill1 = stateAfterThreeMonths!.GetEntityByName<Bill>(bill1Name);
        var bill2 = stateAfterThreeMonths.GetEntityByName<Bill>(bill2Name);
        var bill3 = stateAfterThreeMonths.GetEntityByName<Bill>(bill3Name);
        
        Assert.IsNotNull(bill1);
        Assert.IsNotNull(bill2);
        Assert.IsNotNull(bill3);

        // Each bill should have the LoanPayment category
        Assert.IsTrue(bill1.Items.All(i => i.Category == "LoanPayment"));
        Assert.IsTrue(bill2.Items.All(i => i.Category == "LoanPayment"));
        Assert.IsTrue(bill3.Items.All(i => i.Category == "LoanPayment"));
    }
}
