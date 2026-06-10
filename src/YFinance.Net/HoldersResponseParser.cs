using System.Globalization;
using System.Text.Json;

namespace YFinance.Net;

internal static class HoldersResponseParser
{
    public static HoldersSnapshot Parse(string json, string requestedSymbol)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedSymbol);

        using var document = JsonDocument.Parse(json);
        var result = GetQuoteSummaryResult(document.RootElement);

        return new HoldersSnapshot(
            requestedSymbol,
            ParseMajorHoldersBreakdown(GetObject(result, "majorHoldersBreakdown")),
            ParseOwnershipList(GetObject(result, "institutionOwnership")),
            ParseOwnershipList(GetObject(result, "fundOwnership")));
    }

    private static JsonElement GetQuoteSummaryResult(JsonElement root)
    {
        if (!root.TryGetProperty("quoteSummary", out var quoteSummary) || quoteSummary.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary holders response did not contain a quoteSummary object.");
        }

        if (quoteSummary.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null && error.ValueKind != JsonValueKind.Undefined)
        {
            throw new InvalidOperationException($"Yahoo Finance quoteSummary holders endpoint returned an error: {error}");
        }

        if (!quoteSummary.TryGetProperty("result", out var resultArray) || resultArray.ValueKind != JsonValueKind.Array || resultArray.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary holders endpoint returned no result.");
        }

        return resultArray[0];
    }

    private static MajorHoldersBreakdown? ParseMajorHoldersBreakdown(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new MajorHoldersBreakdown(
            GetDecimal(element.Value, "insidersPercentHeld"),
            GetDecimal(element.Value, "institutionsPercentHeld"),
            GetDecimal(element.Value, "institutionsFloatPercentHeld"),
            GetInt32(element.Value, "institutionsCount"));
    }

    private static HolderPosition[] ParseOwnershipList(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        if (!element.Value.TryGetProperty("ownershipList", out var ownershipList) || ownershipList.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<HolderPosition>();
        foreach (var item in ownershipList.EnumerateArray())
        {
            var organization = GetString(item, "organization");
            if (string.IsNullOrWhiteSpace(organization))
            {
                continue;
            }

            items.Add(new HolderPosition(
                GetDateOnlyFromRawUnix(item, "reportDate"),
                organization,
                GetDecimal(item, "pctHeld"),
                GetInt64(item, "position"),
                GetDecimal(item, "value"),
                GetDecimal(item, "pctChange")));
        }

        return items.ToArray();
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