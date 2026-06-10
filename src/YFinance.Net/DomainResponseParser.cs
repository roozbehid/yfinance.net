using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace YFinance.Net;

internal static class DomainResponseParser
{
    public static SectorDetails ParseSector(string json, string key, string region)
    {
        ArgumentNullException.ThrowIfNull(json);
        return ParseSector(Encoding.UTF8.GetBytes(json), key, region);
    }

    public static SectorDetails ParseSector(ReadOnlyMemory<byte> json, string key, string region)
    {
        var data = ParseDataRoot(json, "sector");
        return new SectorDetails(
            key,
            region,
            data.Name,
            data.Symbol,
            data.Overview,
            data.TopCompanies,
            data.ResearchReports,
            data.TopEtfs,
            data.TopMutualFunds,
            data.Industries);
    }

    public static IndustryDetails ParseIndustry(string json, string key, string region)
    {
        ArgumentNullException.ThrowIfNull(json);
        return ParseIndustry(Encoding.UTF8.GetBytes(json), key, region);
    }

    public static IndustryDetails ParseIndustry(ReadOnlyMemory<byte> json, string key, string region)
    {
        var data = ParseDataRoot(json, "industry");
        return new IndustryDetails(
            key,
            region,
            data.Name,
            data.Symbol,
            data.SectorKey,
            data.SectorName,
            data.Overview,
            data.TopCompanies,
            data.ResearchReports,
            data.TopPerformingCompanies,
            data.TopGrowthCompanies);
    }

    private static DomainData ParseDataRoot(ReadOnlyMemory<byte> json, string endpointName)
    {
        var reader = new Utf8JsonReader(json.Span, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException($"Yahoo Finance {endpointName} response did not contain a data object.");
        }

        DomainData? data = null;
        string? errorMessage = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException($"Yahoo Finance {endpointName} response did not contain a data object.");
            }

            var isFinance = reader.ValueTextEquals("finance"u8);
            var isData = reader.ValueTextEquals("data"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isFinance)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    errorMessage = ParseFinanceObject(ref reader);
                }
                else if (reader.TokenType is JsonTokenType.StartArray)
                {
                    reader.Skip();
                }

                continue;
            }

            if (isData)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new InvalidOperationException($"Yahoo Finance {endpointName} response did not contain a data object.");
                }

                data = ParseDataObject(ref reader);
                continue;
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new InvalidOperationException($"Yahoo Finance {endpointName} endpoint returned an error: {errorMessage}");
        }

        return data ?? throw new InvalidOperationException($"Yahoo Finance {endpointName} response did not contain a data object.");
    }

    private static string? ParseFinanceObject(ref Utf8JsonReader reader)
    {
        string? errorMessage = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance domain response contained an invalid finance object.");
            }

            var isError = reader.ValueTextEquals("error"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isError)
            {
                if (reader.TokenType != JsonTokenType.Null)
                {
                    errorMessage = ReadJsonValueAsString(ref reader);
                }
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return errorMessage;
    }

    private static DomainData ParseDataObject(ref Utf8JsonReader reader)
    {
        var data = new DomainData();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance domain response contained an invalid data object.");
            }

            var isName = reader.ValueTextEquals("name"u8);
            var isSymbol = reader.ValueTextEquals("symbol"u8);
            var isSectorKey = reader.ValueTextEquals("sectorKey"u8);
            var isSectorName = reader.ValueTextEquals("sectorName"u8);
            var isOverview = reader.ValueTextEquals("overview"u8);
            var isTopCompanies = reader.ValueTextEquals("topCompanies"u8);
            var isResearchReports = reader.ValueTextEquals("researchReports"u8);
            var isTopEtfs = reader.ValueTextEquals("topETFs"u8);
            var isTopMutualFunds = reader.ValueTextEquals("topMutualFunds"u8);
            var isIndustries = reader.ValueTextEquals("industries"u8);
            var isTopPerformingCompanies = reader.ValueTextEquals("topPerformingCompanies"u8);
            var isTopGrowthCompanies = reader.ValueTextEquals("topGrowthCompanies"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isName)
            {
                data.Name = ReadNullableString(ref reader);
            }
            else if (isSymbol)
            {
                data.Symbol = ReadNullableString(ref reader);
            }
            else if (isSectorKey)
            {
                data.SectorKey = ReadNullableString(ref reader);
            }
            else if (isSectorName)
            {
                data.SectorName = ReadNullableString(ref reader);
            }
            else if (isOverview)
            {
                data.Overview = reader.TokenType == JsonTokenType.StartObject ? ParseOverviewObject(ref reader) : null;
            }
            else if (isTopCompanies)
            {
                data.TopCompanies = reader.TokenType == JsonTokenType.StartArray ? ParseCompaniesArray(ref reader) : [];
            }
            else if (isResearchReports)
            {
                data.ResearchReports = reader.TokenType == JsonTokenType.StartArray ? ParseResearchReportsArray(ref reader) : [];
            }
            else if (isTopEtfs)
            {
                data.TopEtfs = reader.TokenType == JsonTokenType.StartArray ? ParseSymbolReferencesArray(ref reader) : [];
            }
            else if (isTopMutualFunds)
            {
                data.TopMutualFunds = reader.TokenType == JsonTokenType.StartArray ? ParseSymbolReferencesArray(ref reader) : [];
            }
            else if (isIndustries)
            {
                data.Industries = reader.TokenType == JsonTokenType.StartArray ? ParseIndustriesArray(ref reader) : [];
            }
            else if (isTopPerformingCompanies)
            {
                data.TopPerformingCompanies = reader.TokenType == JsonTokenType.StartArray ? ParseCompaniesArray(ref reader) : [];
            }
            else if (isTopGrowthCompanies)
            {
                data.TopGrowthCompanies = reader.TokenType == JsonTokenType.StartArray ? ParseCompaniesArray(ref reader) : [];
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return data;
    }

    private static DomainOverview ParseOverviewObject(ref Utf8JsonReader reader)
    {
        int? companiesCount = null;
        decimal? marketCap = null;
        string? messageBoardId = null;
        string? description = null;
        int? industriesCount = null;
        decimal? marketWeight = null;
        int? employeeCount = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance domain response contained an invalid overview object.");
            }

            var isCompaniesCount = reader.ValueTextEquals("companiesCount"u8);
            var isMarketCap = reader.ValueTextEquals("marketCap"u8);
            var isMessageBoardId = reader.ValueTextEquals("messageBoardId"u8);
            var isDescription = reader.ValueTextEquals("description"u8);
            var isIndustriesCount = reader.ValueTextEquals("industriesCount"u8);
            var isMarketWeight = reader.ValueTextEquals("marketWeight"u8);
            var isEmployeeCount = reader.ValueTextEquals("employeeCount"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isCompaniesCount)
            {
                companiesCount = ReadNullableInt32(ref reader);
            }
            else if (isMarketCap)
            {
                marketCap = ReadNullableDecimal(ref reader);
            }
            else if (isMessageBoardId)
            {
                messageBoardId = ReadNullableString(ref reader);
            }
            else if (isDescription)
            {
                description = ReadNullableString(ref reader);
            }
            else if (isIndustriesCount)
            {
                industriesCount = ReadNullableInt32(ref reader);
            }
            else if (isMarketWeight)
            {
                marketWeight = ReadNullableDecimal(ref reader);
            }
            else if (isEmployeeCount)
            {
                employeeCount = ReadNullableInt32(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return new DomainOverview(companiesCount, marketCap, messageBoardId, description, industriesCount, marketWeight, employeeCount);
    }

    private static DomainCompany[] ParseCompaniesArray(ref Utf8JsonReader reader)
    {
        var items = new List<DomainCompany>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }

                continue;
            }

            var item = ParseCompanyObject(ref reader);
            var symbol = item.Symbol;
            if (string.IsNullOrWhiteSpace(symbol))
            {
                continue;
            }

            items.Add(item);
        }

        return items.ToArray();
    }

    private static DomainCompany ParseCompanyObject(ref Utf8JsonReader reader)
    {
        string? symbol = null;
        string? name = null;
        string? rating = null;
        decimal? marketWeight = null;
        decimal? marketCap = null;
        decimal? lastPrice = null;
        decimal? targetPrice = null;
        decimal? yearToDateReturn = null;
        decimal? growthEstimate = null;
        decimal? regularMarketChangePercent = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance domain response contained an invalid company object.");
            }

            var isSymbol = reader.ValueTextEquals("symbol"u8);
            var isName = reader.ValueTextEquals("name"u8);
            var isRating = reader.ValueTextEquals("rating"u8);
            var isMarketWeight = reader.ValueTextEquals("marketWeight"u8);
            var isMarketCap = reader.ValueTextEquals("marketCap"u8);
            var isLastPrice = reader.ValueTextEquals("lastPrice"u8);
            var isTargetPrice = reader.ValueTextEquals("targetPrice"u8);
            var isYearToDateReturn = reader.ValueTextEquals("ytdReturn"u8);
            var isGrowthEstimate = reader.ValueTextEquals("growthEstimate"u8);
            var isRegularMarketChangePercent = reader.ValueTextEquals("regMarketChangePercent"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isSymbol)
            {
                symbol = ReadNullableString(ref reader);
            }
            else if (isName)
            {
                name = ReadNullableString(ref reader);
            }
            else if (isRating)
            {
                rating = ReadNullableString(ref reader);
            }
            else if (isMarketWeight)
            {
                marketWeight = ReadNullableDecimal(ref reader);
            }
            else if (isMarketCap)
            {
                marketCap = ReadNullableDecimal(ref reader);
            }
            else if (isLastPrice)
            {
                lastPrice = ReadNullableDecimal(ref reader);
            }
            else if (isTargetPrice)
            {
                targetPrice = ReadNullableDecimal(ref reader);
            }
            else if (isYearToDateReturn)
            {
                yearToDateReturn = ReadNullableDecimal(ref reader);
            }
            else if (isGrowthEstimate)
            {
                growthEstimate = ReadNullableDecimal(ref reader);
            }
            else if (isRegularMarketChangePercent)
            {
                regularMarketChangePercent = ReadNullableDecimal(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return new DomainCompany(
            symbol ?? string.Empty,
            name,
            rating,
            marketWeight,
            marketCap,
            lastPrice,
            targetPrice,
            yearToDateReturn,
            growthEstimate,
            regularMarketChangePercent);
    }

    private static DomainResearchReport[] ParseResearchReportsArray(ref Utf8JsonReader reader)
    {
        var items = new List<DomainResearchReport>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }

                continue;
            }

            items.Add(ParseResearchReportObject(ref reader));
        }

        return items.ToArray();
    }

    private static DomainResearchReport ParseResearchReportObject(ref Utf8JsonReader reader)
    {
        string? id = null;
        string? headHtml = null;
        string? provider = null;
        decimal? targetPrice = null;
        string? targetPriceStatus = null;
        string? investmentRating = null;
        DateTimeOffset? reportDate = null;
        string? reportTitle = null;
        string? reportType = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance domain response contained an invalid research report object.");
            }

            var isId = reader.ValueTextEquals("id"u8);
            var isHeadHtml = reader.ValueTextEquals("headHtml"u8);
            var isProvider = reader.ValueTextEquals("provider"u8);
            var isTargetPrice = reader.ValueTextEquals("targetPrice"u8);
            var isTargetPriceStatus = reader.ValueTextEquals("targetPriceStatus"u8);
            var isInvestmentRating = reader.ValueTextEquals("investmentRating"u8);
            var isReportDate = reader.ValueTextEquals("reportDate"u8);
            var isReportTitle = reader.ValueTextEquals("reportTitle"u8);
            var isReportType = reader.ValueTextEquals("reportType"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isId)
            {
                id = ReadNullableString(ref reader);
            }
            else if (isHeadHtml)
            {
                headHtml = ReadNullableString(ref reader);
            }
            else if (isProvider)
            {
                provider = ReadNullableString(ref reader);
            }
            else if (isTargetPrice)
            {
                targetPrice = ReadNullableDecimal(ref reader);
            }
            else if (isTargetPriceStatus)
            {
                targetPriceStatus = ReadNullableString(ref reader);
            }
            else if (isInvestmentRating)
            {
                investmentRating = ReadNullableString(ref reader);
            }
            else if (isReportDate)
            {
                reportDate = ReadNullableDateTimeOffset(ref reader);
            }
            else if (isReportTitle)
            {
                reportTitle = ReadNullableString(ref reader);
            }
            else if (isReportType)
            {
                reportType = ReadNullableString(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return new DomainResearchReport(id, headHtml, provider, targetPrice, targetPriceStatus, investmentRating, reportDate, reportTitle, reportType);
    }

    private static SymbolReference[] ParseSymbolReferencesArray(ref Utf8JsonReader reader)
    {
        var items = new List<SymbolReference>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }

                continue;
            }

            string? symbol = null;
            string? name = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new InvalidOperationException("Yahoo Finance domain response contained an invalid symbol reference object.");
                }

                var isSymbol = reader.ValueTextEquals("symbol"u8);
                var isName = reader.ValueTextEquals("name"u8);
                if (!reader.Read())
                {
                    break;
                }

                if (isSymbol)
                {
                    symbol = ReadNullableString(ref reader);
                }
                else if (isName)
                {
                    name = ReadNullableString(ref reader);
                }
                else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }
            }

            if (string.IsNullOrWhiteSpace(symbol))
            {
                continue;
            }

            items.Add(new SymbolReference(symbol, name));
        }

        return items.ToArray();
    }

    private static SectorIndustryReference[] ParseIndustriesArray(ref Utf8JsonReader reader)
    {
        var items = new List<SectorIndustryReference>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }

                continue;
            }

            string? key = null;
            string? name = null;
            string? symbol = null;
            decimal? marketWeight = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new InvalidOperationException("Yahoo Finance domain response contained an invalid industry reference object.");
                }

                var isKey = reader.ValueTextEquals("key"u8);
                var isName = reader.ValueTextEquals("name"u8);
                var isSymbol = reader.ValueTextEquals("symbol"u8);
                var isMarketWeight = reader.ValueTextEquals("marketWeight"u8);

                if (!reader.Read())
                {
                    break;
                }

                if (isKey)
                {
                    key = ReadNullableString(ref reader);
                }
                else if (isName)
                {
                    name = ReadNullableString(ref reader);
                }
                else if (isSymbol)
                {
                    symbol = ReadNullableString(ref reader);
                }
                else if (isMarketWeight)
                {
                    marketWeight = ReadNullableDecimal(ref reader);
                }
                else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }
            }

            if (string.IsNullOrWhiteSpace(key) || string.Equals(name, "All Industries", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            items.Add(new SectorIndustryReference(key, name, symbol, marketWeight));
        }

        return items.ToArray();
    }

    private static string? ReadNullableString(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return ReadRawWrappedString(ref reader);
        }

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
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return ReadRawWrappedDecimal(ref reader);
        }

        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number when reader.TryGetDecimal(out var number) => number,
            JsonTokenType.String when decimal.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static int? ReadNullableInt32(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return ReadRawWrappedInt32(ref reader);
        }

        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number when reader.TryGetInt32(out var number) => number,
            JsonTokenType.String when int.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static DateTimeOffset? ReadNullableDateTimeOffset(ref Utf8JsonReader reader)
    {
        var value = ReadNullableString(ref reader);
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;
    }

    private static string? ReadRawWrappedString(ref Utf8JsonReader reader)
    {
        string? result = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance domain response contained an invalid wrapped value.");
            }

            var isRaw = reader.ValueTextEquals("raw"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isRaw)
            {
                result = ReadNullableString(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return result;
    }

    private static decimal? ReadRawWrappedDecimal(ref Utf8JsonReader reader)
    {
        decimal? result = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance domain response contained an invalid wrapped value.");
            }

            var isRaw = reader.ValueTextEquals("raw"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isRaw)
            {
                result = ReadNullableDecimal(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return result;
    }

    private static int? ReadRawWrappedInt32(ref Utf8JsonReader reader)
    {
        int? result = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance domain response contained an invalid wrapped value.");
            }

            var isRaw = reader.ValueTextEquals("raw"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isRaw)
            {
                result = ReadNullableInt32(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return result;
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

    private sealed class DomainData
    {
        public string? Name { get; set; }
        public string? Symbol { get; set; }
        public string? SectorKey { get; set; }
        public string? SectorName { get; set; }
        public DomainOverview? Overview { get; set; }
        public DomainCompany[] TopCompanies { get; set; } = [];
        public DomainResearchReport[] ResearchReports { get; set; } = [];
        public SymbolReference[] TopEtfs { get; set; } = [];
        public SymbolReference[] TopMutualFunds { get; set; } = [];
        public SectorIndustryReference[] Industries { get; set; } = [];
        public DomainCompany[] TopPerformingCompanies { get; set; } = [];
        public DomainCompany[] TopGrowthCompanies { get; set; } = [];
    }
}
