using System;
using System.Collections.Generic;
using LoanSplitter.Domain;
using LoanSplitter.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LoanSplitter.Tests.Events;

[TestClass]
public class EventStreamTest
{

    [TestMethod]
    public void HelloWorld()
    {
        const string acctName = "creditAcct";

        var events = new List<EventBase>()
        {
            new AccountCreatedEvent(new DateTime(2025, 6, 1), acctName),

            new LoanContractedEvent(
                new DateTime(2025,
                    11,
                    1),
                "apartLoan",
                6450000 + 1700100, // todo need to separate the fees.
                4.74,
                360,
                "creditAcct",
                "Alin",
                "Diana"),

            new AdvancePaymentEvent(
                new DateTime(2025, 11, 1),
                "apartLoan",
                new AccountTransaction(1260100, "Alin")),

            new AdvancePaymentEvent(
                new DateTime(2025, 11, 1),
                "apartLoan",
                new AccountTransaction(440000, "Diana")),

            new CorrectNextLoanPaymentEvent(new DateTime(2025, 11, 1),
                "apartLoan",
                8129,
                40764)
        };
        
        var stream = new EventStream(events);
    }
}