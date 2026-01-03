using System;
using System.Collections.Generic;
using System.Text.Json;
using LoanSplitter.Domain;
using LoanSplitter.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LoanSplitter.Tests.Events;

[TestClass]
public class UserEventJsonDeserializerTest
{
    [TestMethod]
    public void CanDeserializeMixedEventList()
    {
        var json = """
                   [
                     { "type": "AccountCreated", "date": "2025-06-01", "acctName": "creditAcct" },
                     {
                       "type": "LoanContracted",
                       "date": "2025-11-01",
                       "loanName": "apartLoan",
                       "principal": 8150100,
                       "nominalRate": 4.74,
                       "term": 360,
                       "backingAccountName": "creditAcct",
                       "name1": "Alin",
                       "name2": "Diana"
                     },
                     {
                       "type": "AdvancePayment",
                       "date": "2025-11-01",
                       "loanName": "apartLoan",
                       "transaction": { "amount": 1260100, "person": "Alin" }
                     }
                   ]
                   """;

        var deserializer = new UserEventJsonDeserializer();
        var events = deserializer.Deserialize(json);

        AssertCount(events, 3);
        Assert.IsInstanceOfType(events[0], typeof(AccountCreatedEvent));
        Assert.IsInstanceOfType(events[1], typeof(LoanContractedEvent));
        Assert.IsInstanceOfType(events[2], typeof(AdvancePaymentEvent));

        Assert.AreEqual(new DateTime(2025, 6, 1), events[0].Date);
    }

    [TestMethod]
    public void UnknownTypeThrowsJsonException()
    {
        var json = """
                   [ { "type": "Nope", "date": "2025-01-01" } ]
                   """;

        var deserializer = new UserEventJsonDeserializer();

        try
        {
            // ReSharper disable once UnusedVariable
            var _ = deserializer.Deserialize(json);
            Assert.Fail("Expected JsonException.");
        }
        catch (System.Text.Json.JsonException)
        {
            // expected
        }
    }

    [TestMethod]
    public void SerializeProducesExpectedShape()
    {
        var events = new List<EventBase>
        {
            new AccountCreatedEvent(new DateTime(2025, 6, 1), "creditAcct"),
            new LoanPaymentEvent(new DateTime(2025, 7, 1), "checking", "apartLoan")
        };

        var serializer = new UserEventJsonDeserializer();
        var json = serializer.Serialize(events);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.AreEqual(JsonValueKind.Array, root.ValueKind);
        Assert.AreEqual(2, root.GetArrayLength());
        Assert.AreEqual("AccountCreated", root[0].GetProperty("type").GetString());
        Assert.AreEqual("creditAcct", root[0].GetProperty("acctName").GetString());
        Assert.AreEqual("LoanPayment", root[1].GetProperty("type").GetString());
        Assert.AreEqual("checking", root[1].GetProperty("fromAccountName").GetString());
        Assert.AreEqual("apartLoan", root[1].GetProperty("loanName").GetString());
    }

    [TestMethod]
    public void SerializeThenDeserializeRoundTrips()
    {
        var advancePayment = new AdvancePaymentEvent(
            new DateTime(2025, 11, 1),
            "apartLoan",
            new AccountTransaction(1260100, "Alin"));

        var correction = new CorrectNextLoanPaymentEvent(
            new DateTime(2025, 11, 1),
            "apartLoan",
            8129,
            40764);

        var rateChanged = new InterestRateChangedEvent(
            new DateTime(2026, 1, 15),
            "apartLoan",
            5.25);

        var serializer = new UserEventJsonDeserializer();
        var json = serializer.Serialize(new List<EventBase> { advancePayment, correction, rateChanged });
        var roundTripped = serializer.Deserialize(json);

        AssertCount(roundTripped, 3);

        var roundAdvance = (AdvancePaymentEvent)roundTripped[0];
        Assert.AreEqual(advancePayment.LoanName, roundAdvance.LoanName);
        Assert.AreEqual(advancePayment.Transaction.Amount, roundAdvance.Transaction.Amount);
        Assert.AreEqual(advancePayment.Transaction.PersonName, roundAdvance.Transaction.PersonName);

        var roundCorrection = (CorrectNextLoanPaymentEvent)roundTripped[1];
        Assert.AreEqual(correction.LoanName, roundCorrection.LoanName);
        Assert.AreEqual(correction.Principal, roundCorrection.Principal);
        Assert.AreEqual(correction.Interest, roundCorrection.Interest);

        var roundRate = (InterestRateChangedEvent)roundTripped[2];
        Assert.AreEqual(rateChanged.LoanName, roundRate.LoanName);
        Assert.AreEqual(rateChanged.Rate, roundRate.Rate);
    }

    private static void AssertCount<T>(ICollection<T> collection, int expected)
    {
        if (collection.Count != expected)
            Assert.Fail($"Expected {expected} items, but got {collection.Count}.");
    }
}
