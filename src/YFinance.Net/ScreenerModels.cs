namespace YFinance.Net;

using System.Buffers;
using System.Globalization;
using System.Text.Json;

/// <summary>
/// Known Yahoo Finance predefined screener identifiers.
/// </summary>
public enum PredefinedScreenerId
{
    /// <summary>
    /// Aggressive small-cap equities.
    /// </summary>
    AggressiveSmallCaps,
    /// <summary>
    /// Daily gainers.
    /// </summary>
    DayGainers,
    /// <summary>
    /// Daily losers.
    /// </summary>
    DayLosers,
    /// <summary>
    /// Growth-oriented technology stocks.
    /// </summary>
    GrowthTechnologyStocks,
    /// <summary>
    /// Most active symbols by volume.
    /// </summary>
    MostActives,
    /// <summary>
    /// Most shorted stocks.
    /// </summary>
    MostShortedStocks,
    /// <summary>
    /// Small-cap gainers.
    /// </summary>
    SmallCapGainers,
    /// <summary>
    /// Undervalued growth stocks.
    /// </summary>
    UndervaluedGrowthStocks,
    /// <summary>
    /// Undervalued large-cap stocks.
    /// </summary>
    UndervaluedLargeCaps,
    /// <summary>
    /// Conservative foreign funds.
    /// </summary>
    ConservativeForeignFunds,
    /// <summary>
    /// High-yield bond funds.
    /// </summary>
    HighYieldBond,
    /// <summary>
    /// Portfolio anchor funds.
    /// </summary>
    PortfolioAnchors,
    /// <summary>
    /// Solid large-growth funds.
    /// </summary>
    SolidLargeGrowthFunds,
    /// <summary>
    /// Solid mid-cap growth funds.
    /// </summary>
    SolidMidcapGrowthFunds,
    /// <summary>
    /// Top mutual funds.
    /// </summary>
    TopMutualFunds,
    /// <summary>
    /// Top US ETFs.
    /// </summary>
    TopEtfsUs,
    /// <summary>
    /// Top performing ETFs.
    /// </summary>
    TopPerformingEtfs,
    /// <summary>
    /// Technology ETFs.
    /// </summary>
    TechnologyEtfs,
    /// <summary>
    /// Bond ETFs.
    /// </summary>
    BondEtfs
}

/// <summary>
/// Helper access to the full set of predefined screener identifiers.
/// </summary>
public static class PredefinedScreeners
{
    /// <summary>
    /// Gets all known predefined screener identifiers.
    /// </summary>
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

/// <summary>
/// Options for querying a Yahoo predefined screener.
/// </summary>
public sealed record PredefinedScreenerOptions
{
    /// <summary>
    /// Maximum number of rows to return.
    /// </summary>
    public int Count { get; init; } = 25;

    /// <summary>
    /// Result offset for paging.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Language header value used by Yahoo Finance.
    /// </summary>
    public string Language { get; init; } = "en-US";

    /// <summary>
    /// Region code used by Yahoo Finance.
    /// </summary>
    public string Region { get; init; } = "US";
}

/// <summary>
/// Yahoo screener quote types.
/// </summary>
public enum ScreenerQuoteType
{
    /// <summary>
    /// Equity instruments.
    /// </summary>
    Equity,
    /// <summary>
    /// Mutual funds.
    /// </summary>
    MutualFund,
    /// <summary>
    /// Exchange-traded funds.
    /// </summary>
    Etf
}

/// <summary>
/// Sort directions supported by Yahoo screener requests.
/// </summary>
public enum ScreenerSortOrder
{
    /// <summary>
    /// Ascending order.
    /// </summary>
    Ascending,
    /// <summary>
    /// Descending order.
    /// </summary>
    Descending
}

/// <summary>
/// Options for executing a custom Yahoo screener query.
/// </summary>
public sealed record ScreenerOptions
{
    /// <summary>
    /// Maximum number of rows to return.
    /// </summary>
    public int Count { get; init; } = 25;

    /// <summary>
    /// Result offset for paging.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Sort field name sent to Yahoo Finance.
    /// </summary>
    public string SortField { get; init; } = "ticker";

    /// <summary>
    /// Sort direction.
    /// </summary>
    public ScreenerSortOrder SortOrder { get; init; } = ScreenerSortOrder.Descending;

    /// <summary>
    /// Quote type to query.
    /// </summary>
    public ScreenerQuoteType QuoteType { get; init; } = ScreenerQuoteType.Equity;

    /// <summary>
    /// Language header value used by Yahoo Finance.
    /// </summary>
    public string Language { get; init; } = "en-US";

    /// <summary>
    /// Region code used by Yahoo Finance.
    /// </summary>
    public string Region { get; init; } = "US";

    /// <summary>
    /// Returns a copy of the options with updated sort settings.
    /// </summary>
    /// <param name="field">Field to sort by.</param>
    /// <param name="sortOrder">Sort direction.</param>
    /// <returns>A new options instance with updated sort settings.</returns>
    public ScreenerOptions WithSort(ScreenerField field, ScreenerSortOrder sortOrder = ScreenerSortOrder.Descending)
    {
        return this with
        {
            SortField = field.Name,
            SortOrder = sortOrder
        };
    }
}

/// <summary>
/// Combines a screener query with execution options.
/// </summary>
public sealed record ScreenerDefinition
{
    /// <summary>
    /// Query expression to execute.
    /// </summary>
    public required ScreenerQuery Query { get; init; }

    /// <summary>
    /// Options used when executing the query.
    /// </summary>
    public required ScreenerOptions Options { get; init; }

    /// <summary>
    /// Returns a copy of the definition with an updated row count.
    /// </summary>
    public ScreenerDefinition WithCount(int count)
    {
        return this with
        {
            Options = Options with { Count = count }
        };
    }

    /// <summary>
    /// Returns a copy of the definition with an updated result offset.
    /// </summary>
    public ScreenerDefinition WithOffset(int offset)
    {
        return this with
        {
            Options = Options with { Offset = offset }
        };
    }

    /// <summary>
    /// Returns a copy of the definition with updated sort settings.
    /// </summary>
    public ScreenerDefinition WithSort(ScreenerField field, ScreenerSortOrder sortOrder = ScreenerSortOrder.Descending)
    {
        return this with
        {
            Options = Options.WithSort(field, sortOrder)
        };
    }

    /// <summary>
    /// Returns a copy of the definition with replacement options.
    /// </summary>
    public ScreenerDefinition WithOptions(ScreenerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return this with { Options = options };
    }
}

/// <summary>
/// Screener results returned by Yahoo Finance.
/// </summary>
/// <param name="Id">Yahoo screener identifier.</param>
/// <param name="Title">Title returned by Yahoo Finance.</param>
/// <param name="Description">Description returned by Yahoo Finance.</param>
/// <param name="CanonicalName">Canonical Yahoo screener name.</param>
/// <param name="Start">Starting offset for this page.</param>
/// <param name="Count">Number of rows returned in this page.</param>
/// <param name="Total">Total number of matching rows reported by Yahoo Finance.</param>
/// <param name="IsPremium">Indicates whether the screener is premium-only on Yahoo Finance.</param>
/// <param name="IconUrl">Optional screener icon URL.</param>
/// <param name="Criteria">Criteria metadata returned by Yahoo Finance.</param>
/// <param name="Quotes">Rows returned by the screener.</param>
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
    /// <summary>
    /// Gets whether another page of results is available.
    /// </summary>
    public bool HasMore => Start + Count < Total;

    /// <summary>
    /// Gets the next offset to request, or <see langword="null"/> when no more rows are available.
    /// </summary>
    public int? NextOffset => HasMore ? Start + Count : null;
}

/// <summary>
/// Metadata describing how a screener result page was produced.
/// </summary>
/// <param name="Size">Requested page size.</param>
/// <param name="Offset">Requested offset.</param>
/// <param name="SortField">Sort field used by Yahoo Finance.</param>
/// <param name="SortType">Sort direction returned by Yahoo Finance.</param>
/// <param name="QuoteType">Quote type returned by Yahoo Finance.</param>
public readonly record struct ScreenerCriteria(
    int Size,
    int Offset,
    string? SortField,
    string? SortType,
    string? QuoteType);

/// <summary>
/// Represents a single screener result row.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="ShortName">Short display name.</param>
/// <param name="LongName">Long display name.</param>
/// <param name="Exchange">Exchange code.</param>
/// <param name="FullExchangeName">Full exchange display name.</param>
/// <param name="QuoteType">Quote type.</param>
/// <param name="TypeDisplayName">Human-readable type name.</param>
/// <param name="Currency">Quote currency.</param>
/// <param name="RegularMarketPrice">Current regular market price.</param>
/// <param name="RegularMarketChange">Absolute regular market change.</param>
/// <param name="RegularMarketChangePercent">Percentage regular market change.</param>
/// <param name="RegularMarketVolume">Regular market volume.</param>
/// <param name="MarketCap">Market capitalization when returned.</param>
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

/// <summary>
/// Represents a value used in a screener comparison expression.
/// </summary>
public readonly record struct ScreenerValue
{
    private readonly object? _value;

    /// <summary>
    /// Initializes a string screener value.
    /// </summary>
    public ScreenerValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    /// <summary>
    /// Initializes an integer screener value.
    /// </summary>
    public ScreenerValue(int value)
    {
        _value = value;
    }

    /// <summary>
    /// Initializes a 64-bit integer screener value.
    /// </summary>
    public ScreenerValue(long value)
    {
        _value = value;
    }

    /// <summary>
    /// Initializes a decimal screener value.
    /// </summary>
    public ScreenerValue(decimal value)
    {
        _value = value;
    }

    /// <summary>
    /// Initializes a double screener value.
    /// </summary>
    public ScreenerValue(double value)
    {
        _value = value;
    }

    /// <summary>
    /// Initializes a boolean screener value.
    /// </summary>
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

    /// <summary>
    /// Converts a string to a screener value.
    /// </summary>
    public static implicit operator ScreenerValue(string value) => new(value);

    /// <summary>
    /// Converts an integer to a screener value.
    /// </summary>
    public static implicit operator ScreenerValue(int value) => new(value);

    /// <summary>
    /// Converts a 64-bit integer to a screener value.
    /// </summary>
    public static implicit operator ScreenerValue(long value) => new(value);

    /// <summary>
    /// Converts a decimal to a screener value.
    /// </summary>
    public static implicit operator ScreenerValue(decimal value) => new(value);

    /// <summary>
    /// Converts a double to a screener value.
    /// </summary>
    public static implicit operator ScreenerValue(double value) => new(value);

    /// <summary>
    /// Converts a boolean to a screener value.
    /// </summary>
    public static implicit operator ScreenerValue(bool value) => new(value);
}

/// <summary>
/// Base type for building custom Yahoo Finance screener query expressions.
/// </summary>
public abstract class ScreenerQuery
{
    /// <summary>
    /// Combines two or more queries with a logical AND.
    /// </summary>
    public static ScreenerQuery And(params ScreenerQuery[] queries)
    {
        ValidateCompositeOperands(queries, nameof(queries));
        return new CompositeScreenerQuery("AND", queries);
    }

    /// <summary>
    /// Combines two or more queries with a logical OR.
    /// </summary>
    public static ScreenerQuery Or(params ScreenerQuery[] queries)
    {
        ValidateCompositeOperands(queries, nameof(queries));
        return new CompositeScreenerQuery("OR", queries);
    }

    /// <summary>
    /// Creates an equality comparison for a raw Yahoo field name.
    /// </summary>
    public static ScreenerQuery Equal(string field, ScreenerValue value)
    {
        return new ComparisonScreenerQuery("EQ", field, [value]);
    }

    /// <summary>
    /// Creates an equality comparison for a strongly typed screener field.
    /// </summary>
    public static ScreenerQuery Equal(ScreenerField field, ScreenerValue value)
    {
        return Equal(field.Name, value);
    }

    /// <summary>
    /// Creates a greater-than comparison for a raw Yahoo field name.
    /// </summary>
    public static ScreenerQuery GreaterThan(string field, ScreenerValue value)
    {
        return new ComparisonScreenerQuery("GT", field, [value]);
    }

    /// <summary>
    /// Creates a greater-than comparison for a strongly typed screener field.
    /// </summary>
    public static ScreenerQuery GreaterThan(ScreenerField field, ScreenerValue value)
    {
        return GreaterThan(field.Name, value);
    }

    /// <summary>
    /// Creates a greater-than-or-equal comparison for a raw Yahoo field name.
    /// </summary>
    public static ScreenerQuery GreaterThanOrEqual(string field, ScreenerValue value)
    {
        return new ComparisonScreenerQuery("GTE", field, [value]);
    }

    /// <summary>
    /// Creates a greater-than-or-equal comparison for a strongly typed screener field.
    /// </summary>
    public static ScreenerQuery GreaterThanOrEqual(ScreenerField field, ScreenerValue value)
    {
        return GreaterThanOrEqual(field.Name, value);
    }

    /// <summary>
    /// Creates a less-than comparison for a raw Yahoo field name.
    /// </summary>
    public static ScreenerQuery LessThan(string field, ScreenerValue value)
    {
        return new ComparisonScreenerQuery("LT", field, [value]);
    }

    /// <summary>
    /// Creates a less-than comparison for a strongly typed screener field.
    /// </summary>
    public static ScreenerQuery LessThan(ScreenerField field, ScreenerValue value)
    {
        return LessThan(field.Name, value);
    }

    /// <summary>
    /// Creates a less-than-or-equal comparison for a raw Yahoo field name.
    /// </summary>
    public static ScreenerQuery LessThanOrEqual(string field, ScreenerValue value)
    {
        return new ComparisonScreenerQuery("LTE", field, [value]);
    }

    /// <summary>
    /// Creates a less-than-or-equal comparison for a strongly typed screener field.
    /// </summary>
    public static ScreenerQuery LessThanOrEqual(ScreenerField field, ScreenerValue value)
    {
        return LessThanOrEqual(field.Name, value);
    }

    /// <summary>
    /// Creates an inclusive between comparison for a raw Yahoo field name.
    /// </summary>
    public static ScreenerQuery Between(string field, ScreenerValue lowerInclusive, ScreenerValue upperInclusive)
    {
        return new ComparisonScreenerQuery("BTWN", field, [lowerInclusive, upperInclusive]);
    }

    /// <summary>
    /// Creates an inclusive between comparison for a strongly typed screener field.
    /// </summary>
    public static ScreenerQuery Between(ScreenerField field, ScreenerValue lowerInclusive, ScreenerValue upperInclusive)
    {
        return Between(field.Name, lowerInclusive, upperInclusive);
    }

    /// <summary>
    /// Creates an OR group of equality comparisons for a raw Yahoo field name.
    /// </summary>
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

    /// <summary>
    /// Creates an OR group of equality comparisons for a strongly typed screener field.
    /// </summary>
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