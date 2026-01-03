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

        var events = new List<EventBase>
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

    var state = stream.GetStateForDate(new DateTime(2026, 6, 1));
    Assert.IsNotNull(state);
    var loan = state!.GetEntityByName<Loan>("apartLoan");
        var alin = loan.SubLoans["Alin"];
        var diana = loan.SubLoans["Diana"];

        loan.GetNextMonthlyPayment();

        // parsing logic => from a list of events.
        // Reduce term events -> needs to recalculate the values.
        // UX: create separate timelines
        // UX: compare specific time ranges (not infinite).


        // Now, what do I want the stream to tell me?
        // At a certain date,
        //   -> Loan - paid off, remaining, interest thus far. / split by person
        //   -> Apartment - current value? / split by person.
        //   -> Accounts
        //   -> Upcoming payment for each individual.

        // Other features to add:
        // Bill. 
        // Felleskosts
        // Other bills (electricity).

        // For the UX layer:
        //   -> Next month's payment.
        //   -> What if I make this advance payment at time X?
        //   -> What if interest changes at time X?
        //   -> 
    }
}