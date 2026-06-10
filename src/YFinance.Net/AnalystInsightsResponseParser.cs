using System.Globalization;
using System.Text.Json;

namespace YFinance.Net;

internal static class AnalystInsightsResponseParser
{
    public static AnalystInsights Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var result = GetQuoteSummaryResult(document.RootElement);

        var price = GetObject(result, "price");
        var financialData = GetObject(result, "financialData");
        var recommendationTrend = GetObject(result, "recommendationTrend");
        var upgradeDowngradeHistory = GetObject(result, "upgradeDowngradeHistory");
        var earningsHistory = GetObject(result, "earningsHistory");
        var earningsTrend = GetObject(result, "earningsTrend");
        var industryTrend = GetObject(result, "industryTrend");
        var sectorTrend = GetObject(result, "sectorTrend");
        var indexTrend = GetObject(result, "indexTrend");

        var symbol = GetString(price, "symbol") ?? GetString(result, "symbol");
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary analytics response did not contain a symbol.");
        }

        var earningsTrendItems = GetArray(earningsTrend, "trend");
        return new AnalystInsights(
            symbol,
            new AnalystPriceTargets(
                GetDecimal(financialData, "currentPrice"),
                GetDecimal(financialData, "targetLowPrice"),
                GetDecimal(financialData, "targetHighPrice"),
                GetDecimal(financialData, "targetMeanPrice"),
                GetDecimal(financialData, "targetMedianPrice")),
            ParseRecommendations(GetArray(recommendationTrend, "trend")),
            ParseUpgradesDowngrades(GetArray(upgradeDowngradeHistory, "history")),
            ParseEarningsHistory(GetArray(earningsHistory, "history")),
            ParseEarningsEstimates(earningsTrendItems),
            ParseRevenueEstimates(earningsTrendItems),
            ParseEpsTrends(earningsTrendItems),
            ParseEpsRevisions(earningsTrendItems),
            ParseGrowthEstimates(earningsTrendItems, industryTrend, sectorTrend, indexTrend));
    }

    private static JsonElement GetQuoteSummaryResult(JsonElement root)
    {
        if (!root.TryGetProperty("quoteSummary", out var quoteSummary) || quoteSummary.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary analytics response did not contain a quoteSummary object.");
        }

        if (quoteSummary.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null && error.ValueKind != JsonValueKind.Undefined)
        {
            throw new InvalidOperationException($"Yahoo Finance quoteSummary analytics endpoint returned an error: {error}");
        }

        if (!quoteSummary.TryGetProperty("result", out var resultArray) || resultArray.ValueKind != JsonValueKind.Array || resultArray.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary analytics endpoint returned no result.");
        }

        return resultArray[0];
    }

    private static IReadOnlyList<RecommendationTrendEntry> ParseRecommendations(JsonElement? trendArray)
    {
        if (trendArray is null)
        {
            return [];
        }

        var items = new List<RecommendationTrendEntry>();
        foreach (var item in trendArray.Value.EnumerateArray())
        {
            var period = GetString(item, "period");
            if (string.IsNullOrWhiteSpace(period))
            {
                continue;
            }

            items.Add(new RecommendationTrendEntry(
                period,
                GetInt32(item, "strongBuy"),
                GetInt32(item, "buy"),
                GetInt32(item, "hold"),
                GetInt32(item, "sell"),
                GetInt32(item, "strongSell")));
        }

        return items;
    }

    private static IReadOnlyList<EarningsHistoryEntry> ParseEarningsHistory(JsonElement? historyArray)
    {
        if (historyArray is null)
        {
            return [];
        }

        var items = new List<EarningsHistoryEntry>();
        foreach (var item in historyArray.Value.EnumerateArray())
        {
            items.Add(new EarningsHistoryEntry(
                GetString(item, "period"),
                GetDateTimeOffsetFromRawUnix(item, "quarter"),
                GetDecimal(item, "epsActual"),
                GetDecimal(item, "epsEstimate"),
                GetDecimal(item, "epsDifference"),
                GetDecimal(item, "surprisePercent"),
                GetString(item, "currency")));
        }

        return items;
    }

    private static IReadOnlyList<UpgradeDowngradeEntry> ParseUpgradesDowngrades(JsonElement? historyArray)
    {
        if (historyArray is null)
        {
            return [];
        }

        var items = new List<UpgradeDowngradeEntry>();
        foreach (var item in historyArray.Value.EnumerateArray())
        {
            items.Add(new UpgradeDowngradeEntry(
                GetDateTimeOffsetFromUnix(item, "epochGradeDate"),
                GetString(item, "firm"),
                GetString(item, "toGrade"),
                GetString(item, "fromGrade"),
                GetString(item, "action"),
                GetString(item, "priceTargetAction"),
                GetDecimal(item, "currentPriceTarget"),
                GetDecimal(item, "priorPriceTarget")));
        }

        return items;
    }

    private static IReadOnlyList<PeriodicEarningsEstimate> ParseEarningsEstimates(JsonElement? trendArray)
    {
        if (trendArray is null)
        {
            return [];
        }

        var items = new List<PeriodicEarningsEstimate>();
        foreach (var item in trendArray.Value.EnumerateArray())
        {
            var period = GetString(item, "period");
            if (string.IsNullOrWhiteSpace(period))
            {
                continue;
            }

            var estimate = GetObject(item, "earningsEstimate");
            items.Add(new PeriodicEarningsEstimate(
                period,
                GetDateOnly(item, "endDate"),
                GetInt32(estimate, "numberOfAnalysts"),
                GetDecimal(estimate, "avg"),
                GetDecimal(estimate, "low"),
                GetDecimal(estimate, "high"),
                GetDecimal(estimate, "yearAgoEps"),
                GetDecimal(estimate, "growth"),
                GetString(estimate, "earningsCurrency")));
        }

        return items;
    }

    private static IReadOnlyList<PeriodicRevenueEstimate> ParseRevenueEstimates(JsonElement? trendArray)
    {
        if (trendArray is null)
        {
            return [];
        }

        var items = new List<PeriodicRevenueEstimate>();
        foreach (var item in trendArray.Value.EnumerateArray())
        {
            var period = GetString(item, "period");
            if (string.IsNullOrWhiteSpace(period))
            {
                continue;
            }

            var estimate = GetObject(item, "revenueEstimate");
            items.Add(new PeriodicRevenueEstimate(
                period,
                GetDateOnly(item, "endDate"),
                GetInt32(estimate, "numberOfAnalysts"),
                GetDecimal(estimate, "avg"),
                GetDecimal(estimate, "low"),
                GetDecimal(estimate, "high"),
                GetDecimal(estimate, "yearAgoRevenue"),
                GetDecimal(estimate, "growth"),
                GetString(estimate, "revenueCurrency")));
        }

        return items;
    }

    private static IReadOnlyList<PeriodicEpsTrend> ParseEpsTrends(JsonElement? trendArray)
    {
        if (trendArray is null)
        {
            return [];
        }

        var items = new List<PeriodicEpsTrend>();
        foreach (var item in trendArray.Value.EnumerateArray())
        {
            var period = GetString(item, "period");
            if (string.IsNullOrWhiteSpace(period))
            {
                continue;
            }

            var trend = GetObject(item, "epsTrend");
            items.Add(new PeriodicEpsTrend(
                period,
                GetDateOnly(item, "endDate"),
                GetDecimal(trend, "current"),
                GetDecimal(trend, "7daysAgo"),
                GetDecimal(trend, "30daysAgo"),
                GetDecimal(trend, "60daysAgo"),
                GetDecimal(trend, "90daysAgo"),
                GetString(trend, "epsTrendCurrency")));
        }

        return items;
    }

    private static IReadOnlyList<PeriodicEpsRevisions> ParseEpsRevisions(JsonElement? trendArray)
    {
        if (trendArray is null)
        {
            return [];
        }

        var items = new List<PeriodicEpsRevisions>();
        foreach (var item in trendArray.Value.EnumerateArray())
        {
            var period = GetString(item, "period");
            if (string.IsNullOrWhiteSpace(period))
            {
                continue;
            }

            var revisions = GetObject(item, "epsRevisions");
            items.Add(new PeriodicEpsRevisions(
                period,
                GetDateOnly(item, "endDate"),
                GetInt32(revisions, "upLast7days"),
                GetInt32(revisions, "upLast30days"),
                GetInt32(revisions, "downLast7Days") ?? GetInt32(revisions, "downLast7days"),
                GetInt32(revisions, "downLast30days"),
                GetInt32(revisions, "downLast90days"),
                GetString(revisions, "epsRevisionsCurrency")));
        }

        return items;
    }

    private static IReadOnlyList<GrowthEstimate> ParseGrowthEstimates(JsonElement? earningsTrendArray, JsonElement? industryTrend, JsonElement? sectorTrend, JsonElement? indexTrend)
    {
        var rows = new Dictionary<string, GrowthEstimateBuilder>(StringComparer.OrdinalIgnoreCase);

        if (earningsTrendArray is not null)
        {
            foreach (var item in earningsTrendArray.Value.EnumerateArray())
            {
                var period = GetString(item, "period");
                if (string.IsNullOrWhiteSpace(period))
                {
                    continue;
                }

                var row = GetOrCreate(rows, period);
                row.Stock = GetDecimal(item, "growth");
            }
        }

        ApplyTrendEstimates(rows, industryTrend, static (row, value) => row.Industry = value);
        ApplyTrendEstimates(rows, sectorTrend, static (row, value) => row.Sector = value);
        ApplyTrendEstimates(rows, indexTrend, static (row, value) => row.Index = value);

        return rows.Values
            .Select(static row => new GrowthEstimate(row.Period, row.Stock, row.Industry, row.Sector, row.Index))
            .ToArray();
    }

    private static void ApplyTrendEstimates(Dictionary<string, GrowthEstimateBuilder> rows, JsonElement? trendRoot, Action<GrowthEstimateBuilder, decimal?> assign)
    {
        var estimates = GetArray(trendRoot, "estimates");
        if (estimates is null)
        {
            return;
        }

        foreach (var item in estimates.Value.EnumerateArray())
        {
            var period = GetString(item, "period");
            if (string.IsNullOrWhiteSpace(period))
            {
                continue;
            }

            var row = GetOrCreate(rows, period);
            assign(row, GetDecimal(item, "growth"));
        }
    }

    private static GrowthEstimateBuilder GetOrCreate(Dictionary<string, GrowthEstimateBuilder> rows, string period)
    {
        if (!rows.TryGetValue(period, out var row))
        {
            row = new GrowthEstimateBuilder(period);
            rows[period] = row;
        }

        return row;
    }

    private static JsonElement? GetObject(JsonElement? element, string propertyName)
    {
        if (element is not null && element.Value.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object)
        {
            return value;
        }

        return null;
    }

    private static JsonElement? GetArray(JsonElement? element, string propertyName)
    {
        if (element is not null && element.Value.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array)
        {
            return value;
        }

        return null;
    }

    private static string? GetString(JsonElement? element, string propertyName)
    {
        if (element is null || !element.Value.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("raw", out var rawValue))
        {
            value = rawValue;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            _ => value.ToString()
        };
    }

    private static decimal? GetDecimal(JsonElement? element, string propertyName)
    {
        if (element is null || !element.Value.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("raw", out var rawValue))
        {
            value = rawValue;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static int? GetInt32(JsonElement? element, string propertyName)
    {
        if (element is null || !element.Value.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("raw", out var rawValue))
        {
            value = rawValue;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static DateOnly? GetDateOnly(JsonElement? element, string propertyName)
    {
        var value = GetString(element, propertyName);
        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }

    private static DateTimeOffset? GetDateTimeOffsetFromRawUnix(JsonElement? element, string propertyName)
    {
        if (element is null || !element.Value.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("raw", out var rawValue))
        {
            value = rawValue;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var unixSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }

        return null;
    }

    private static DateTimeOffset? GetDateTimeOffsetFromUnix(JsonElement? element, string propertyName)
    {
        if (element is null || !element.Value.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var unixSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }

        if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out unixSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }

        return null;
    }

    private sealed class GrowthEstimateBuilder(string period)
    {
        public string Period { get; } = period;

        public decimal? Stock { get; set; }

        public decimal? Industry { get; set; }

        public decimal? Sector { get; set; }

        public decimal? Index { get; set; }
    }
}