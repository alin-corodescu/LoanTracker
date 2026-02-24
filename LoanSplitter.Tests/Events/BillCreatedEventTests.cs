using LoanSplitter.Domain;
using LoanSplitter.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LoanSplitter.Tests.Events;

[TestClass]
public class BillCreatedEventTests
{
    [TestMethod]
    public void BillCreatedEvent_ShouldCreateBillAndUpdateBalances()
    {
        // Arrange
        var initialState = new State(new Dictionary<string, object>());

        var billItems = new List<BillItem>
        {
            new BillItem(500.0, "Alin", "Groceries"),
            new BillItem(300.0, "Diana", "Groceries")
        };

        var forAccounts = new Dictionary<string, double>
        {
            { "Alin", 0.625 },  // 500 / 800
            { "Diana", 0.375 }  // 300 / 800
        };

        var billEvent = new BillAddedEvent(
            new DateTime(2025, 11, 1),
            "groceries_november",
            "November groceries",
            billItems,
            "creditAcct",
            "creditAcct",
            forAccounts
        );

        // Act
        var outcome = billEvent.Apply(initialState);
        var newState = initialState.WithUpdates(outcome.StateUpdates);

        // Assert
        Assert.IsNotNull(outcome);
        Assert.AreEqual(2, outcome.StateUpdates.Count);

        // Check bill was created
        var bill = newState.GetEntityByName<Bill>("groceries_november");
        Assert.IsNotNull(bill);
        Assert.AreEqual("November groceries", bill.Description);
        Assert.AreEqual(2, bill.Items.Count);
        Assert.AreEqual(800.0, bill.TotalAmount);

        // Check bill items
        Assert.AreEqual(500.0, bill.Items[0].Amount);
        Assert.AreEqual("Alin", bill.Items[0].PersonName);
        Assert.AreEqual("Groceries", bill.Items[0].Category);

        Assert.AreEqual(300.0, bill.Items[1].Amount);
        Assert.AreEqual("Diana", bill.Items[1].PersonName);
        Assert.AreEqual("Groceries", bill.Items[1].Category);

        // Check balances were updated: both Alin and Diana owe creditAcct
        var balances = newState.GetEntityByName<PersonBalances>(PersonBalances.StateKey);
        Assert.AreEqual(500.0, balances.Balances["Alin"]["creditAcct"]);
        Assert.AreEqual(300.0, balances.Balances["Diana"]["creditAcct"]);

        // Account should NOT have transactions (bills no longer create account transactions)
        var account = newState.GetEntityByName<Account>("creditAcct");
        Assert.AreEqual(0, account.Transactions.Count);
    }

    [TestMethod]
    public void BillCreatedEvent_WithMultipleCategories_ShouldWork()
    {
        // Arrange
        var initialState = new State(new Dictionary<string, object>
        {
            { "creditAcct", new Account() }
        });

        var billItems = new List<BillItem>
        {
            new BillItem(1200.0, "Alin", "Utilities"),
            new BillItem(450.0, "Diana", "Internet"),
            new BillItem(650.0, "Alin", "Groceries")
        };

        var forAccounts = new Dictionary<string, double>
        {
            { "Alin", 0.804348 },  // (1200 + 650) / 2300
            { "Diana", 0.195652 }  // 450 / 2300
        };

        var billEvent = new BillAddedEvent(
            new DateTime(2025, 12, 1),
            "household_december",
            "December household expenses",
            billItems,
            "creditAcct",
            "creditAcct",
            forAccounts
        );

        // Act
        var outcome = billEvent.Apply(initialState);
        var newState = initialState.WithUpdates(outcome.StateUpdates);

        // Assert
        var bill = newState.GetEntityByName<Bill>("household_december");
        Assert.AreEqual(3, bill.Items.Count);
        Assert.AreEqual(2300.0, bill.TotalAmount);

        // Verify categories
        Assert.AreEqual("Utilities", bill.Items[0].Category);
        Assert.AreEqual("Internet", bill.Items[1].Category);
        Assert.AreEqual("Groceries", bill.Items[2].Category);
    }

    [TestMethod]
    public void Bill_CreateSplitBill_ShouldSplitByShares()
    {
        // Arrange
        var shares = new Dictionary<string, double>
        {
            { "Alin", 0.6 },
            { "Diana", 0.4 }
        };

        // Act
        var bill = Bill.CreateSplitBill(
            "Utilities December",
            "Utilities",
            1000.0,
            shares,
            "creditAcct"
        );

        // Assert
        Assert.AreEqual("Utilities December", bill.Description);
        Assert.AreEqual(2, bill.Items.Count);
        Assert.AreEqual(1000.0, bill.TotalAmount);

        var alinItem = bill.Items.First(i => i.PersonName == "Alin");
        var dianaItem = bill.Items.First(i => i.PersonName == "Diana");

        Assert.AreEqual(600.0, alinItem.Amount);
        Assert.AreEqual("Utilities", alinItem.Category);
        Assert.AreEqual(400.0, dianaItem.Amount);
        Assert.AreEqual("Utilities", dianaItem.Category);
    }

    [TestMethod]
    public void Bill_ApplyToAccount_ShouldAddTransactions()
    {
        // Arrange
        var account = new Account();
        var billItems = new List<BillItem>
        {
            new BillItem(500.0, "Alin", "Groceries"),
            new BillItem(300.0, "Diana", "Groceries")
        };
        var forAccounts = new Dictionary<string, double>
        {
            { "Alin", 0.625 },
            { "Diana", 0.375 }
        };
        var bill = new Bill("Groceries", billItems, "creditAcct", forAccounts);

        // Act
        var updatedAccount = bill.ApplyToAccount(account);

        // Assert
        Assert.AreEqual(2, updatedAccount.Transactions.Count);
        Assert.AreEqual(500.0, updatedAccount.Transactions[0].Amount);
        Assert.AreEqual("Alin", updatedAccount.Transactions[0].PersonName);
        Assert.AreEqual(300.0, updatedAccount.Transactions[1].Amount);
        Assert.AreEqual("Diana", updatedAccount.Transactions[1].PersonName);
    }

    [TestMethod]
    public void Bill_CreateSplitBill_WithInvalidShares_ShouldThrow()
    {
        // Arrange
        var shares = new Dictionary<string, double>
        {
            { "Alin", 0.6 },
            { "Diana", 0.3 } // Sum is 0.9, not 1.0
        };

        // Act & Assert
        try
        {
            Bill.CreateSplitBill(
                "Utilities",
                "Utilities",
                1000.0,
                shares,
                "creditAcct"
            );
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.IsTrue(ex.Message.Contains("sum to 1.0"));
        }
    }

    [TestMethod]
    public void Bill_WithoutItems_ShouldCreateTransactionsFromShares()
    {
        // Arrange
        var account = new Account();
        var shares = new Dictionary<string, double>
        {
            { "Alin", 0.6 },
            { "Diana", 0.4 }
        };
        
        // Create bill without itemization
        var bill = new Bill("Restaurant bill", 1000.0, "creditAcct", shares);

        // Act
        var updatedAccount = bill.ApplyToAccount(account);

        // Assert
        Assert.AreEqual(2, updatedAccount.Transactions.Count);
        Assert.AreEqual(600.0, updatedAccount.Transactions[0].Amount);
        Assert.AreEqual("Alin", updatedAccount.Transactions[0].PersonName);
        Assert.AreEqual(400.0, updatedAccount.Transactions[1].Amount);
        Assert.AreEqual("Diana", updatedAccount.Transactions[1].PersonName);
        Assert.AreEqual(1000.0, bill.TotalAmount);
        Assert.AreEqual(0, bill.Items.Count);
    }
}
