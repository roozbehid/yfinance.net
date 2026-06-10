using System.Globalization;
using System.Text.Json;

namespace YFinance.Net;

internal static class InsiderDataResponseParser
{
    public static InsiderSnapshot Parse(string json, string requestedSymbol)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedSymbol);

        using var document = JsonDocument.Parse(json);
        var result = GetQuoteSummaryResult(document.RootElement);

        return new InsiderSnapshot(
            requestedSymbol,
            ParseTransactions(GetObject(result, "insiderTransactions")),
            ParseRosterHolders(GetObject(result, "insiderHolders")),
            ParsePurchaseActivity(GetObject(result, "netSharePurchaseActivity")));
    }

    private static JsonElement GetQuoteSummaryResult(JsonElement root)
    {
        if (!root.TryGetProperty("quoteSummary", out var quoteSummary) || quoteSummary.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary insider response did not contain a quoteSummary object.");
        }

        if (quoteSummary.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null && error.ValueKind != JsonValueKind.Undefined)
        {
            throw new InvalidOperationException($"Yahoo Finance quoteSummary insider endpoint returned an error: {error}");
        }

        if (!quoteSummary.TryGetProperty("result", out var resultArray) || resultArray.ValueKind != JsonValueKind.Array || resultArray.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary insider endpoint returned no result.");
        }

        return resultArray[0];
    }

    private static InsiderTransaction[] ParseTransactions(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        if (!element.Value.TryGetProperty("transactions", out var transactions) || transactions.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<InsiderTransaction>();
        foreach (var item in transactions.EnumerateArray())
        {
            var insider = GetString(item, "filerName");
            if (string.IsNullOrWhiteSpace(insider))
            {
                continue;
            }

            items.Add(new InsiderTransaction(
                GetDateOnlyFromRawUnix(item, "startDate"),
                insider,
                GetString(item, "filerRelation"),
                GetString(item, "filerUrl"),
                GetString(item, "moneyText"),
                GetString(item, "transactionText"),
                GetInt64(item, "shares"),
                GetDecimal(item, "value"),
                GetString(item, "ownership")));
        }

        return items.ToArray();
    }

    private static InsiderRosterHolder[] ParseRosterHolders(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        if (!element.Value.TryGetProperty("holders", out var holders) || holders.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<InsiderRosterHolder>();
        foreach (var item in holders.EnumerateArray())
        {
            var name = GetString(item, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            items.Add(new InsiderRosterHolder(
                name,
                GetString(item, "relation"),
                GetString(item, "url"),
                GetString(item, "transactionDescription"),
                GetDateOnlyFromRawUnix(item, "latestTransDate"),
                GetDateOnlyFromRawUnix(item, "positionDirectDate"),
                GetInt64(item, "positionDirect"),
                GetDateOnlyFromRawUnix(item, "positionIndirectDate"),
                GetInt64(item, "positionIndirect")));
        }

        return items.ToArray();
    }

    private static NetSharePurchaseActivity? ParsePurchaseActivity(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new NetSharePurchaseActivity(
            GetString(element.Value, "period"),
            GetInt32(element.Value, "buyInfoCount"),
            GetInt64(element.Value, "buyInfoShares"),
            GetDecimal(element.Value, "buyPercentInsiderShares"),
            GetInt32(element.Value, "sellInfoCount"),
            GetInt64(element.Value, "sellInfoShares"),
            GetDecimal(element.Value, "sellPercentInsiderShares"),
            GetInt32(element.Value, "netInfoCount"),
            GetInt64(element.Value, "netInfoShares"),
            GetDecimal(element.Value, "netPercentInsiderShares"),
            GetInt64(element.Value, "totalInsiderShares"),
            GetInt64(element.Value, "netInstSharesBuying"),
            GetDecimal(element.Value, "netInstBuyingPercent"));
    }

    private static JsonElement? GetObject(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object
            ? value
            : null;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static int? GetInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return TryReadWrappedValue(value, out var wrapped)
            ? ReadInt32(wrapped)
            : ReadInt32(value);
    }

    private static long? GetInt64(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return TryReadWrappedValue(value, out var wrapped)
            ? ReadInt64(wrapped)
            : ReadInt64(value);
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return TryReadWrappedValue(value, out var wrapped)
            ? ReadDecimal(wrapped)
            : ReadDecimal(value);
    }

    private static DateOnly? GetDateOnlyFromRawUnix(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (TryReadWrappedValue(value, out var wrapped))
        {
            var unix = ReadInt64(wrapped);
            return unix is null ? null : DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(unix.Value).UtcDateTime);
        }

        return null;
    }

    private static bool TryReadWrappedValue(JsonElement value, out JsonElement wrapped)
    {
        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("raw", out wrapped))
        {
            return true;
        }

        wrapped = default;
        return false;
    }

    private static int? ReadInt32(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
        {
            return intValue;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var longValue) && longValue is >= int.MinValue and <= int.MaxValue)
        {
            return (int)longValue;
        }

        return null;
    }

    private static long? ReadInt64(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var longValue))
        {
            return longValue;
        }

        return null;
    }

    private static decimal? ReadDecimal(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.Number => Convert.ToDecimal(value.GetDouble(), CultureInfo.InvariantCulture),
            _ => null
        };
    }
}