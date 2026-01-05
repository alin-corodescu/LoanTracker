using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LoanSplitter.Tests.Api;

[TestClass]
public sealed class EventStreamEndpointsTests
{
    private WebApplicationFactory<Program>? _factory;

    [TestInitialize]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [TestCleanup]
    public void TearDown()
    {
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task CreateEventStreamAndQueryState()
    {
        var client = EnsureClient();

        const string newStreamPayload = """
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
  },
  {
    "type": "AdvancePayment",
    "date": "2025-11-01",
    "loanName": "apartLoan",
    "transaction": { "amount": 440000, "person": "Diana" }
  }
]
""";

        var createResponse = await client.PostAsync("/eventStream",
            new StringContent(newStreamPayload, Encoding.UTF8, "application/json"));
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateEventStreamResponse>();
        Assert.IsNotNull(created);
        Assert.AreNotEqual(Guid.Empty, created.Id);

        var queryResponse = await client.GetAsync($"/eventStream/{created.Id}?date=2026-06-01");
        queryResponse.EnsureSuccessStatusCode();

        await using var contentStream = await queryResponse.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(contentStream);
        var root = document.RootElement;

        Assert.IsTrue(root.TryGetProperty("entities", out var entities));
        Assert.IsTrue(entities.TryGetProperty("apartLoan", out var apartLoan));
        Assert.IsTrue(apartLoan.TryGetProperty("remainingAmount", out var remainingAmount));
  Assert.IsGreaterThan(0d, remainingAmount.GetDouble());
    }

    [TestMethod]
    public async Task MissingDateQueryReturnsBadRequest()
    {
        var client = EnsureClient();

        var response = await client.GetAsync($"/eventStream/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task GetEventsUpToDate()
    {
        var client = EnsureClient();

        const string newStreamPayload = """
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
  }
]
""";

        var createResponse = await client.PostAsync("/eventStream",
            new StringContent(newStreamPayload, Encoding.UTF8, "application/json"));
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateEventStreamResponse>();
        Assert.IsNotNull(created);

        // Query events up to a date that should include some system-generated LoanPayment events
        var queryResponse = await client.GetAsync($"/eventStream/{created.Id}/events?date=2026-01-01");
        queryResponse.EnsureSuccessStatusCode();

        var eventsJson = await queryResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(eventsJson);
        var root = document.RootElement;

        Assert.AreEqual(JsonValueKind.Array, root.ValueKind);
        
        // Should have AccountCreated, LoanContracted, and at least one LoanPayment (system generated)
        var eventTypes = new List<string>();
        foreach (var element in root.EnumerateArray())
        {
            if (element.TryGetProperty("type", out var typeProp))
            {
                eventTypes.Add(typeProp.GetString()!);
            }
        }

        CollectionAssert.Contains(eventTypes, "AccountCreated");
        CollectionAssert.Contains(eventTypes, "LoanContracted");
        CollectionAssert.Contains(eventTypes, "LoanPayment", "Should contain system-generated LoanPayment events");
    }

    private HttpClient EnsureClient()
    {
        if (_factory is null) throw new InvalidOperationException("Test factory is not initialized.");
        return _factory.CreateClient();
    }

    private sealed record CreateEventStreamResponse(Guid Id);
}
