namespace YFinance.Net;

using System.Buffers;
using System.Globalization;
using System.Text.Json;

public enum PredefinedScreenerId
{
    AggressiveSmallCaps,
    DayGainers,
    DayLosers,
    GrowthTechnologyStocks,
    MostActives,
    MostShortedStocks,
    SmallCapGainers,
    UndervaluedGrowthStocks,
    UndervaluedLargeCaps,
    ConservativeForeignFunds,
    HighYieldBond,
    PortfolioAnchors,
    SolidLargeGrowthFunds,
    SolidMidcapGrowthFunds,
    TopMutualFunds,
    TopEtfsUs,
    TopPerformingEtfs,
    TechnologyEtfs,
    BondEtfs
}

public static class PredefinedScreeners
{
    public static IReadOnlyList<PredefinedScreenerId> All { get; } =
    [
        PredefinedScreenerId.AggressiveSmallCaps,
        PredefinedScreenerId.DayGainers,
        PredefinedScreenerId.DayLosers,
        PredefinedScreenerId.GrowthTechnologyStocks,
        PredefinedScreenerId.MostActives,
        PredefinedScreenerId.MostShortedStocks,
        PredefinedScreenerId.SmallCapGainers,
        PredefinedScreenerId.UndervaluedGrowthStocks,
        PredefinedScreenerId.UndervaluedLargeCaps,
        PredefinedScreenerId.ConservativeForeignFunds,
        PredefinedScreenerId.HighYieldBond,
        PredefinedScreenerId.PortfolioAnchors,
        PredefinedScreenerId.SolidLargeGrowthFunds,
        PredefinedScreenerId.SolidMidcapGrowthFunds,
        PredefinedScreenerId.TopMutualFunds,
        PredefinedScreenerId.TopEtfsUs,
        PredefinedScreenerId.TopPerformingEtfs,
        PredefinedScreenerId.TechnologyEtfs,
        PredefinedScreenerId.BondEtfs
    ];
}

public sealed record PredefinedScreenerOptions
{
    public int Count { get; init; } = 25;

    public int Offset { get; init; }

    public string Language { get; init; } = "en-US";

    public string Region { get; init; } = "US";
}

public enum ScreenerQuoteType
{
    Equity,
    MutualFund,
    Etf
}

public enum ScreenerSortOrder
{
    Ascending,
    Descending
}

public sealed record ScreenerOptions
{
    public int Count { get; init; } = 25;

    public int Offset { get; init; }

    public string SortField { get; init; } = "ticker";

    public ScreenerSortOrder SortOrder { get; init; } = ScreenerSortOrder.Descending;

    public ScreenerQuoteType QuoteType { get; init; } = ScreenerQuoteType.Equity;

    public string Language { get; init; } = "en-US";

    public string Region { get; init; } = "US";

    public ScreenerOptions WithSort(ScreenerField field, ScreenerSortOrder sortOrder = ScreenerSortOrder.Descending)
    {
        return this with
        {
            SortField = field.Name,
            SortOrder = sortOrder
        };
    }
}

public sealed record ScreenerDefinition
{
    public required ScreenerQuery Query { get; init; }

    public required ScreenerOptions Options { get; init; }

    public ScreenerDefinition WithCount(int count)
    {
        return this with
        {
            Options = Options with { Count = count }
        };
    }

    public ScreenerDefinition WithOffset(int offset)
    {
        return this with
        {
            Options = Options with { Offset = offset }
        };
    }

    public ScreenerDefinition WithSort(ScreenerField field, ScreenerSortOrder sortOrder = ScreenerSortOrder.Descending)
    {
        return this with
        {
            Options = Options.WithSort(field, sortOrder)
        };
    }

    public ScreenerDefinition WithOptions(ScreenerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return this with { Options = options };
    }
}

public sealed record ScreenerResult(
    string? Id,
    string? Title,
    string? Description,
    string? CanonicalName,
    int Start,
    int Count,
    int Total,
    bool IsPremium,
    string? IconUrl,
    ScreenerCriteria Criteria,
    ScreenerQuote[] Quotes)
{
    public bool HasMore => Start + Count < Total;

    public int? NextOffset => HasMore ? Start + Count : null;
}

public readonly record struct ScreenerCriteria(
    int Size,
    int Offset,
    string? SortField,
    string? SortType,
    string? QuoteType);

public readonly record struct ScreenerQuote(
    string Symbol,
    string? ShortName,
    string? LongName,
    string? Exchange,
    string? FullExchangeName,
    string? QuoteType,
    string? TypeDisplayName,
    string? Currency,
    decimal? RegularMarketPrice,
    decimal? RegularMarketChange,
    decimal? RegularMarketChangePercent,
    long? RegularMarketVolume,
    decimal? MarketCap);

public readonly record struct ScreenerValue
{
    private readonly object? _value;

    public ScreenerValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    public ScreenerValue(int value)
    {
        _value = value;
    }

    public ScreenerValue(long value)
    {
        _value = value;
    }

    public ScreenerValue(decimal value)
    {
        _value = value;
    }

    public ScreenerValue(double value)
    {
        _value = value;
    }

    public ScreenerValue(bool value)
    {
        _value = value;
    }

    internal void WriteTo(Utf8JsonWriter writer)
    {
        switch (_value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string stringValue:
                writer.WriteStringValue(stringValue);
                break;
            case int intValue:
                writer.WriteNumberValue(intValue);
                break;
            case long longValue:
                writer.WriteNumberValue(longValue);
                break;
            case decimal decimalValue:
                writer.WriteNumberValue(decimalValue);
                break;
            case double doubleValue:
                writer.WriteNumberValue(doubleValue);
                break;
            case bool boolValue:
                writer.WriteBooleanValue(boolValue);
                break;
            default:
                throw new InvalidOperationException($"Unsupported screener value type '{_value.GetType().Name}'.");
        }
    }

    public static implicit operator ScreenerValue(string value) => new(value);

    public static implicit operator ScreenerValue(int value) => new(value);

    public static implicit operator ScreenerValue(long value) => new(value);

    public static implicit operator ScreenerValue(decimal value) => new(value);

    public static implicit operator ScreenerValue(double value) => new(value);

    public static implicit operator ScreenerValue(bool value) => new(value);
}

public abstract class ScreenerQuery
{
    public static ScreenerQuery And(params ScreenerQuery[] queries)
    {
        ValidateCompositeOperands(queries, nameof(queries));
        return new CompositeScreenerQuery("AND", queries);
    }

    public static ScreenerQuery Or(params ScreenerQuery[] queries)
    {
        ValidateCompositeOperands(queries, nameof(queries));
        return new CompositeScreenerQuery("OR", queries);
    }

    public static ScreenerQuery Equal(string field, ScreenerValue value)
    {
        return new ComparisonScreenerQuery("EQ", field, [value]);
    }

    public static ScreenerQuery Equal(ScreenerField field, ScreenerValue value)
    {
        return Equal(field.Name, value);
    }

    public static ScreenerQuery GreaterThan(string field, ScreenerValue value)
    {
        return new ComparisonScreenerQuery("GT", field, [value]);
    }

    public static ScreenerQuery GreaterThan(ScreenerField field, ScreenerValue value)
    {
        return GreaterThan(field.Name, value);
    }

    public static ScreenerQuery GreaterThanOrEqual(string field, ScreenerValue value)
    {
        return new ComparisonScreenerQuery("GTE", field, [value]);
    }

    public static ScreenerQuery GreaterThanOrEqual(ScreenerField field, ScreenerValue value)
    {
        return GreaterThanOrEqual(field.Name, value);
    }

    public static ScreenerQuery LessThan(string field, ScreenerValue value)
    {
        return new ComparisonScreenerQuery("LT", field, [value]);
    }

    public static ScreenerQuery LessThan(ScreenerField field, ScreenerValue value)
    {
        return LessThan(field.Name, value);
    }

    public static ScreenerQuery LessThanOrEqual(string field, ScreenerValue value)
    {
        return new ComparisonScreenerQuery("LTE", field, [value]);
    }

    public static ScreenerQuery LessThanOrEqual(ScreenerField field, ScreenerValue value)
    {
        return LessThanOrEqual(field.Name, value);
    }

    public static ScreenerQuery Between(string field, ScreenerValue lowerInclusive, ScreenerValue upperInclusive)
    {
        return new ComparisonScreenerQuery("BTWN", field, [lowerInclusive, upperInclusive]);
    }

    public static ScreenerQuery Between(ScreenerField field, ScreenerValue lowerInclusive, ScreenerValue upperInclusive)
    {
        return Between(field.Name, lowerInclusive, upperInclusive);
    }

    public static ScreenerQuery AnyOf(string field, params ScreenerValue[] values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length == 0)
        {
            throw new ArgumentException("At least one value is required.", nameof(values));
        }

        var queries = new ScreenerQuery[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            queries[index] = Equal(field, values[index]);
        }

        return queries.Length == 1 ? queries[0] : Or(queries);
    }

    public static ScreenerQuery AnyOf(ScreenerField field, params ScreenerValue[] values)
    {
        return AnyOf(field.Name, values);
    }

    internal abstract void WriteTo(Utf8JsonWriter writer);

    internal byte[] Serialize()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        WriteTo(writer);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    private static void ValidateCompositeOperands(ScreenerQuery[] queries, string paramName)
    {
        ArgumentNullException.ThrowIfNull(queries);

        if (queries.Length < 2)
        {
            throw new ArgumentException("At least two child queries are required.", paramName);
        }

        for (var index = 0; index < queries.Length; index++)
        {
            ArgumentNullException.ThrowIfNull(queries[index]);
        }
    }

    private sealed class CompositeScreenerQuery(string op, IReadOnlyList<ScreenerQuery> operands) : ScreenerQuery
    {
        internal override void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("operator", op);
            writer.WritePropertyName("operands");
            writer.WriteStartArray();
            foreach (var operand in operands)
            {
                operand.WriteTo(writer);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }

    private sealed class ComparisonScreenerQuery(string op, string field, IReadOnlyList<ScreenerValue> values) : ScreenerQuery
    {
        internal override void WriteTo(Utf8JsonWriter writer)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(field);

            writer.WriteStartObject();
            writer.WriteString("operator", op);
            writer.WritePropertyName("operands");
            writer.WriteStartArray();
            writer.WriteStringValue(field);
            foreach (var value in values)
            {
                value.WriteTo(writer);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}

internal static class PredefinedScreenerIdExtensions
{
    public static string ToWireValue(this PredefinedScreenerId screenId)
    {
        return screenId switch
        {
            PredefinedScreenerId.AggressiveSmallCaps => "aggressive_small_caps",
            PredefinedScreenerId.DayGainers => "day_gainers",
            PredefinedScreenerId.DayLosers => "day_losers",
            PredefinedScreenerId.GrowthTechnologyStocks => "growth_technology_stocks",
            PredefinedScreenerId.MostActives => "most_actives",
            PredefinedScreenerId.MostShortedStocks => "most_shorted_stocks",
            PredefinedScreenerId.SmallCapGainers => "small_cap_gainers",
            PredefinedScreenerId.UndervaluedGrowthStocks => "undervalued_growth_stocks",
            PredefinedScreenerId.UndervaluedLargeCaps => "undervalued_large_caps",
            PredefinedScreenerId.ConservativeForeignFunds => "conservative_foreign_funds",
            PredefinedScreenerId.HighYieldBond => "high_yield_bond",
            PredefinedScreenerId.PortfolioAnchors => "portfolio_anchors",
            PredefinedScreenerId.SolidLargeGrowthFunds => "solid_large_growth_funds",
            PredefinedScreenerId.SolidMidcapGrowthFunds => "solid_midcap_growth_funds",
            PredefinedScreenerId.TopMutualFunds => "top_mutual_funds",
            PredefinedScreenerId.TopEtfsUs => "top_etfs_us",
            PredefinedScreenerId.TopPerformingEtfs => "top_performing_etfs",
            PredefinedScreenerId.TechnologyEtfs => "technology_etfs",
            PredefinedScreenerId.BondEtfs => "bond_etfs",
            _ => throw new ArgumentOutOfRangeException(nameof(screenId), screenId, "Unknown predefined screener id.")
        };
    }
}

internal static class ScreenerQuoteTypeExtensions
{
    public static string ToWireValue(this ScreenerQuoteType quoteType)
    {
        return quoteType switch
        {
            ScreenerQuoteType.Equity => "EQUITY",
            ScreenerQuoteType.MutualFund => "MUTUALFUND",
            ScreenerQuoteType.Etf => "ETF",
            _ => throw new ArgumentOutOfRangeException(nameof(quoteType), quoteType, "Unknown screener quote type.")
        };
    }
}

internal static class ScreenerSortOrderExtensions
{
    public static string ToWireValue(this ScreenerSortOrder sortOrder)
    {
        return sortOrder switch
        {
            ScreenerSortOrder.Ascending => "ASC",
            ScreenerSortOrder.Descending => "DESC",
            _ => throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder, "Unknown screener sort order.")
        };
    }
}