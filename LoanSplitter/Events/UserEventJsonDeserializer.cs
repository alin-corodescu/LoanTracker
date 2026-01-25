using System.Text.Json;
using System.Text.Json.Serialization;
using LoanSplitter.Domain;

namespace LoanSplitter.Events;

/// <summary>
/// Deserializes a JSON array of user events into concrete <see cref="EventBase"/> instances.
///
/// Expected JSON shape (array):
/// [
///   { "type": "AccountCreated", "date": "2025-06-01", "acctName": "creditAcct" },
///   { "type": "LoanContracted", "date": "2025-11-01", "loanName": "apartLoan", ... }
/// ]
///
/// Notes:
/// - Discriminator property is required and is called "type".
/// - Property names are case-insensitive.
/// </summary>
public sealed class UserEventJsonDeserializer
{
    private readonly JsonSerializerOptions _options;

    public UserEventJsonDeserializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? CreateDefaultOptions();
    }

    public List<EventBase> Deserialize(string json)
    {
        if (json is null) throw new ArgumentNullException(nameof(json));

        var result = JsonSerializer.Deserialize<List<EventBase>>(json, _options);
        return result ?? throw new JsonException("JSON did not produce an event list.");
    }

    public async Task<List<EventBase>> DeserializeAsync(Stream jsonStream, CancellationToken cancellationToken = default)
    {
        if (jsonStream is null) throw new ArgumentNullException(nameof(jsonStream));

        var result = await JsonSerializer.DeserializeAsync<List<EventBase>>(jsonStream, _options, cancellationToken);
        return result ?? throw new JsonException("JSON did not produce an event list.");
    }

    public string Serialize(IEnumerable<EventBase> events)
    {
        if (events is null) throw new ArgumentNullException(nameof(events));

        return JsonSerializer.Serialize(events, _options);
    }

    public Task SerializeAsync(IEnumerable<EventBase> events, Stream destination, CancellationToken cancellationToken = default)
    {
        if (events is null) throw new ArgumentNullException(nameof(events));
        if (destination is null) throw new ArgumentNullException(nameof(destination));

        return JsonSerializer.SerializeAsync(destination, events, _options, cancellationToken);
    }

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        options.Converters.Add(new EventBaseJsonConverter());
        return options;
    }

    /// <summary>
    /// Polymorphic converter using a discriminator field called "type".
    /// This avoids requiring settable properties on event types.
    /// </summary>
    private sealed class EventBaseJsonConverter : JsonConverter<EventBase>
    {
        public override bool CanConvert(Type typeToConvert) => typeof(EventBase).IsAssignableFrom(typeToConvert);

        public override EventBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                throw new JsonException("Each event must be a JSON object.");

            var type = GetRequiredString(root, "type");
            var date = GetRequiredDateTime(root, "date");

            // Permit a few aliases to keep JSON friendly.
            return type switch
            {
                "AccountCreated" or "accountCreated" =>
                    new AccountCreatedEvent(date, GetRequiredString(root, "acctName")),

                "AccountTransaction" or "accountTransaction" =>
                    new AccountTransactionEvent(
                        date,
                        GetRequiredString(root, "acctName"),
                        ReadAccountTransaction(root, "transaction")),

                "AdvancePayment" or "advancePayment" =>
                    new AdvancePaymentEvent(
                        date,
                        GetRequiredString(root, "loanName"),
                        ReadAccountTransaction(root, "transaction")),

                "BillCreated" or "billCreated" =>
                    new BillCreatedEvent(
                        date,
                        GetRequiredString(root, "billName"),
                        GetRequiredString(root, "description"),
                        ReadBillItems(root, "items"),
                        GetRequiredString(root, "acctName")),

                "CorrectNextLoanPayment" or "correctNextLoanPayment" =>
                    new CorrectNextLoanPaymentEvent(
                        date,
                        GetRequiredString(root, "loanName"),
                        GetRequiredDouble(root, "principal"),
                        GetRequiredDouble(root, "interest")),

                "CorrectNextLoanPaymentSplit" or "correctNextLoanPaymentSplit" =>
                    new CorrectNextLoanPaymentSplitEvent(
                        date,
                        GetRequiredString(root, "loanName"),
                        ReadContributionOverrides(root, "contributions")),

                "InterestRateChanged" or "interestRateChanged" =>
                    new InterestRateChangedEvent(
                        date,
                        GetRequiredString(root, "loanName"),
                        GetRequiredDouble(root, "rate")),

                "LoanContracted" or "loanContracted" =>
                    new LoanContractedEvent(
                        date,
                        GetRequiredString(root, "loanName"),
                        GetRequiredDouble(root, "principal"),
                        GetRequiredDouble(root, "nominalRate"),
                        GetRequiredInt(root, "term"),
                        GetRequiredString(root, "backingAccountName"),
                        GetRequiredString(root, "name1"),
                        GetRequiredString(root, "name2")),

                "LoanPayment" or "loanPayment" =>
                    new LoanPaymentEvent(
                        date,
                        GetRequiredString(root, "fromAccountName"),
                        GetRequiredString(root, "loanName")),

                _ => throw new JsonException($"Unknown event type '{type}'.")
            };
        }

        public override void Write(Utf8JsonWriter writer, EventBase value, JsonSerializerOptions options)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            writer.WriteStartObject();
            writer.WriteString("date", value.Date);

            switch (value)
            {
                case AccountCreatedEvent created:
                    writer.WriteString("type", "AccountCreated");
                    writer.WriteString("acctName", created.AccountName);
                    break;

                case AccountTransactionEvent accountTransaction:
                    writer.WriteString("type", "AccountTransaction");
                    writer.WriteString("acctName", accountTransaction.AccountName);
                    writer.WritePropertyName("transaction");
                    WriteAccountTransaction(writer, accountTransaction.Transaction);
                    break;

                case BillCreatedEvent billCreated:
                    writer.WriteString("type", "BillCreated");
                    writer.WriteString("billName", billCreated.BillName);
                    writer.WriteString("description", billCreated.Description);
                    writer.WriteString("acctName", billCreated.AccountName);
                    writer.WritePropertyName("items");
                    WriteBillItems(writer, billCreated.Items);
                    break;

                case AdvancePaymentEvent advancePayment:
                    writer.WriteString("type", "AdvancePayment");
                    writer.WriteString("loanName", advancePayment.LoanName);
                    writer.WritePropertyName("transaction");
                    WriteAccountTransaction(writer, advancePayment.Transaction);
                    break;

                case CorrectNextLoanPaymentEvent correction:
                    writer.WriteString("type", "CorrectNextLoanPayment");
                    writer.WriteString("loanName", correction.LoanName);
                    writer.WriteNumber("principal", correction.Principal);
                    writer.WriteNumber("interest", correction.Interest);
                    break;

                case CorrectNextLoanPaymentSplitEvent splitCorrection:
                    writer.WriteString("type", "CorrectNextLoanPaymentSplit");
                    writer.WriteString("loanName", splitCorrection.LoanName);
                    writer.WritePropertyName("contributions");
                    WriteContributionOverrides(writer, splitCorrection.Contributions);
                    break;

                case InterestRateChangedEvent interestRateChanged:
                    writer.WriteString("type", "InterestRateChanged");
                    writer.WriteString("loanName", interestRateChanged.LoanName);
                    writer.WriteNumber("rate", interestRateChanged.Rate);
                    break;

                case LoanContractedEvent contracted:
                    writer.WriteString("type", "LoanContracted");
                    writer.WriteString("loanName", contracted.LoanName);
                    writer.WriteNumber("principal", contracted.Principal);
                    writer.WriteNumber("nominalRate", contracted.NominalRate);
                    writer.WriteNumber("term", contracted.Term);
                    writer.WriteString("backingAccountName", contracted.BackingAccountName);
                    writer.WriteString("name1", contracted.Name1);
                    writer.WriteString("name2", contracted.Name2);
                    break;

                case LoanPaymentEvent loanPayment:
                    writer.WriteString("type", "LoanPayment");
                    writer.WriteString("fromAccountName", loanPayment.FromAccountName);
                    writer.WriteString("loanName", loanPayment.LoanName);
                    break;

                default:
                    throw new NotSupportedException($"Serialization is not supported for type '{value.GetType().Name}'.");
            }

            writer.WriteEndObject();
        }

        private static string GetRequiredString(JsonElement obj, string propertyName)
        {
            if (!obj.TryGetProperty(propertyName, out var element))
                throw new JsonException($"Missing required property '{propertyName}'.");

            if (element.ValueKind != JsonValueKind.String)
                throw new JsonException($"Property '{propertyName}' must be a string.");

            return element.GetString()!;
        }

        private static double GetRequiredDouble(JsonElement obj, string propertyName)
        {
            if (!obj.TryGetProperty(propertyName, out var element))
                throw new JsonException($"Missing required property '{propertyName}'.");

            return element.ValueKind switch
            {
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.String when double.TryParse(element.GetString(), out var d) => d,
                _ => throw new JsonException($"Property '{propertyName}' must be a number.")
            };
        }

        private static int GetRequiredInt(JsonElement obj, string propertyName)
        {
            if (!obj.TryGetProperty(propertyName, out var element))
                throw new JsonException($"Missing required property '{propertyName}'.");

            return element.ValueKind switch
            {
                JsonValueKind.Number => element.GetInt32(),
                JsonValueKind.String when int.TryParse(element.GetString(), out var i) => i,
                _ => throw new JsonException($"Property '{propertyName}' must be an integer.")
            };
        }

        private static DateTime GetRequiredDateTime(JsonElement obj, string propertyName)
        {
            if (!obj.TryGetProperty(propertyName, out var element))
                throw new JsonException($"Missing required property '{propertyName}'.");

            if (element.ValueKind == JsonValueKind.String && DateTime.TryParse(element.GetString(), out var dt))
                return dt;

            throw new JsonException($"Property '{propertyName}' must be a date string.");
        }

        private static AccountTransaction ReadAccountTransaction(JsonElement obj, string propertyName)
        {
            if (!obj.TryGetProperty(propertyName, out var element))
                throw new JsonException($"Missing required property '{propertyName}'.");

            if (element.ValueKind != JsonValueKind.Object)
                throw new JsonException($"Property '{propertyName}' must be an object.");

            if (!element.TryGetProperty("amount", out var amountEl))
                throw new JsonException("Transaction missing required property 'amount'.");

            if (!element.TryGetProperty("person", out var personEl))
                throw new JsonException("Transaction missing required property 'person'.");

            var amount = amountEl.ValueKind switch
            {
                JsonValueKind.Number => amountEl.GetDouble(),
                JsonValueKind.String when double.TryParse(amountEl.GetString(), out var d) => d,
                _ => throw new JsonException("Transaction property 'amount' must be a number.")
            };

            if (personEl.ValueKind != JsonValueKind.String)
                throw new JsonException("Transaction property 'person' must be a string.");

            return new AccountTransaction(amount, personEl.GetString()!);
        }

        private static void WriteAccountTransaction(Utf8JsonWriter writer, AccountTransaction transaction)
        {
            writer.WriteStartObject();
            writer.WriteNumber("amount", transaction.Amount);
            writer.WriteString("person", transaction.PersonName);
            writer.WriteEndObject();
        }

        private static IReadOnlyDictionary<string, double> ReadContributionOverrides(JsonElement obj, string propertyName)
        {
            if (!obj.TryGetProperty(propertyName, out var element))
                throw new JsonException($"Missing required property '{propertyName}'.");

            if (element.ValueKind != JsonValueKind.Object)
                throw new JsonException($"Property '{propertyName}' must be an object.");

            var contributions = new Dictionary<string, double>();

            foreach (var property in element.EnumerateObject())
            {
                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.Number => property.Value.GetDouble(),
                    JsonValueKind.String when double.TryParse(property.Value.GetString(), out var d) => d,
                    _ => throw new JsonException($"Contribution '{property.Name}' must be a number.")
                };

                contributions[property.Name] = value;
            }

            if (contributions.Count == 0)
                throw new JsonException($"Property '{propertyName}' must contain at least one contribution entry.");

            return contributions;
        }

        private static void WriteContributionOverrides(Utf8JsonWriter writer, IReadOnlyDictionary<string, double> contributions)
        {
            writer.WriteStartObject();

            foreach (var kvp in contributions)
                writer.WriteNumber(kvp.Key, kvp.Value);

            writer.WriteEndObject();
        }

        private static IReadOnlyList<BillItem> ReadBillItems(JsonElement obj, string propertyName)
        {
            if (!obj.TryGetProperty(propertyName, out var element))
                throw new JsonException($"Missing required property '{propertyName}'.");

            if (element.ValueKind != JsonValueKind.Array)
                throw new JsonException($"Property '{propertyName}' must be an array.");

            var items = new List<BillItem>();

            foreach (var itemEl in element.EnumerateArray())
            {
                if (itemEl.ValueKind != JsonValueKind.Object)
                    throw new JsonException("Each bill item must be an object.");

                if (!itemEl.TryGetProperty("amount", out var amountEl))
                    throw new JsonException("Bill item missing required property 'amount'.");

                if (!itemEl.TryGetProperty("person", out var personEl))
                    throw new JsonException("Bill item missing required property 'person'.");

                if (!itemEl.TryGetProperty("category", out var categoryEl))
                    throw new JsonException("Bill item missing required property 'category'.");

                var amount = amountEl.ValueKind switch
                {
                    JsonValueKind.Number => amountEl.GetDouble(),
                    JsonValueKind.String when double.TryParse(amountEl.GetString(), out var d) => d,
                    _ => throw new JsonException("Bill item property 'amount' must be a number.")
                };

                if (personEl.ValueKind != JsonValueKind.String)
                    throw new JsonException("Bill item property 'person' must be a string.");

                if (categoryEl.ValueKind != JsonValueKind.String)
                    throw new JsonException("Bill item property 'category' must be a string.");

                items.Add(new BillItem(amount, personEl.GetString()!, categoryEl.GetString()!));
            }

            if (items.Count == 0)
                throw new JsonException($"Property '{propertyName}' must contain at least one bill item.");

            return items;
        }

        private static void WriteBillItems(Utf8JsonWriter writer, IReadOnlyList<BillItem> items)
        {
            writer.WriteStartArray();

            foreach (var item in items)
            {
                writer.WriteStartObject();
                writer.WriteNumber("amount", item.Amount);
                writer.WriteString("person", item.PersonName);
                writer.WriteString("category", item.Category);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
}
