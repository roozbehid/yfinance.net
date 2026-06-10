using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace YFinance.Net;

internal static class LookupResponseParser
{
    public static LookupResult Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Parse(Encoding.UTF8.GetBytes(json));
    }

    public static LookupResult Parse(ReadOnlyMemory<byte> json)
    {
        var reader = new Utf8JsonReader(json.Span, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            return new LookupResult([]);
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance lookup response did not contain a finance object.");
            }

            if (reader.ValueTextEquals("finance"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    return new LookupResult([]);
                }

                return ParseFinanceObject(ref reader);
            }

            SkipPropertyValue(ref reader);
        }

        return new LookupResult([]);
    }

    private static LookupResult ParseFinanceObject(ref Utf8JsonReader reader)
    {
        LookupDocument[] documents = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance lookup response did not contain a finance object.");
            }

            var isError = reader.ValueTextEquals("error"u8);
            var isResult = reader.ValueTextEquals("result"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isError)
            {
                if (reader.TokenType != JsonTokenType.Null)
                {
                    throw new InvalidOperationException($"Yahoo Finance lookup endpoint returned an error: {ReadJsonValueAsString(ref reader)}");
                }

                continue;
            }

            if (isResult)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    return new LookupResult([]);
                }

                documents = ParseResultArray(ref reader);
                continue;
            }

            if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
            {
                reader.Skip();
            }
        }

        return new LookupResult(documents);
    }

    private static LookupDocument[] ParseResultArray(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            return [];
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            return [];
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            SkipRemainingArrayValues(ref reader);
            return [];
        }

        var documents = ParseLookupResultObject(ref reader);
        SkipRemainingArrayValues(ref reader);
        return documents;
    }

    private static LookupDocument[] ParseLookupResultObject(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance lookup result contained an invalid document root.");
            }

            if (reader.ValueTextEquals("documents"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                {
                    return [];
                }

                return ParseDocumentsArray(ref reader);
            }

            SkipPropertyValue(ref reader);
        }

        return [];
    }

    private static LookupDocument[] ParseDocumentsArray(ref Utf8JsonReader reader)
    {
        var documents = new List<LookupDocument>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
                {
                    reader.Skip();
                }

                continue;
            }

            string? symbol = null;
            string? companyName = null;
            string? exchange = null;
            string? type = null;
            string? score = null;
            decimal? price = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new InvalidOperationException("Yahoo Finance lookup documents array contained an invalid document item.");
                }

                var isSymbol = reader.ValueTextEquals("symbol"u8);
                var isCompanyName = reader.ValueTextEquals("companyName"u8);
                var isExchange = reader.ValueTextEquals("exchange"u8);
                var isType = reader.ValueTextEquals("typeDisp"u8);
                var isScore = reader.ValueTextEquals("score"u8);
                var isPrice = reader.ValueTextEquals("price"u8);

                if (!reader.Read())
                {
                    break;
                }

                if (isSymbol)
                {
                    symbol = ReadNullableString(ref reader);
                }
                else if (isCompanyName)
                {
                    companyName = ReadNullableString(ref reader);
                }
                else if (isExchange)
                {
                    exchange = ReadNullableString(ref reader);
                }
                else if (isType)
                {
                    type = ReadNullableString(ref reader);
                }
                else if (isScore)
                {
                    score = ReadNullableString(ref reader);
                }
                else if (isPrice)
                {
                    price = ReadNullableDecimal(ref reader);
                }
                else if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
                {
                    reader.Skip();
                }
            }

            if (string.IsNullOrWhiteSpace(symbol))
            {
                continue;
            }

            documents.Add(new LookupDocument(symbol, companyName, exchange, type, score, price));
        }

        return documents.ToArray();
    }

    private static string? ReadNullableString(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => ReadScalarText(ref reader),
            JsonTokenType.True => bool.TrueString,
            JsonTokenType.False => bool.FalseString,
            _ => null
        };
    }

    private static decimal? ReadNullableDecimal(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number when reader.TryGetDecimal(out var number) => number,
            JsonTokenType.String when decimal.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static string ReadJsonValueAsString(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => "null",
            JsonTokenType.String => reader.GetString() ?? string.Empty,
            JsonTokenType.Number => ReadScalarText(ref reader),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.StartObject or JsonTokenType.StartArray => JsonDocument.ParseValue(ref reader).RootElement.ToString(),
            _ => string.Empty
        };
    }

    private static string ReadScalarText(ref Utf8JsonReader reader)
    {
        if (reader.HasValueSequence)
        {
            return Encoding.UTF8.GetString(reader.ValueSequence.ToArray());
        }

        return Encoding.UTF8.GetString(reader.ValueSpan);
    }

    private static void SkipPropertyValue(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            return;
        }

        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray or JsonTokenType.PropertyName)
        {
            reader.Skip();
        }
    }

    private static void SkipRemainingArrayValues(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return;
            }

            reader.Skip();
        }
    }
}