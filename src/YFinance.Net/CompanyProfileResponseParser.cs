using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace YFinance.Net;

internal static class CompanyProfileResponseParser
{
    public static CompanyProfile Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Parse(Encoding.UTF8.GetBytes(json));
    }

    public static CompanyProfile Parse(ReadOnlyMemory<byte> json)
    {
        var reader = new Utf8JsonReader(json.Span, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary response did not contain a quoteSummary object.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance quoteSummary response did not contain a quoteSummary object.");
            }

            if (reader.ValueTextEquals("quoteSummary"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new InvalidOperationException("Yahoo Finance quoteSummary response did not contain a quoteSummary object.");
                }

                return ParseQuoteSummaryObject(ref reader);
            }

            SkipPropertyValue(ref reader);
        }

        throw new InvalidOperationException("Yahoo Finance quoteSummary response did not contain a quoteSummary object.");
    }

    private static CompanyProfile ParseQuoteSummaryObject(ref Utf8JsonReader reader)
    {
        CompanyProfile? result = null;
        string? errorMessage = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance quoteSummary response did not contain a quoteSummary object.");
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
                    errorMessage = ReadJsonValueAsString(ref reader);
                }

                continue;
            }

            if (isResult)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new InvalidOperationException("Yahoo Finance quoteSummary endpoint returned no result.");
                }

                result = ParseResultArray(ref reader);
                continue;
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new InvalidOperationException($"Yahoo Finance quoteSummary endpoint returned an error: {errorMessage}");
        }

        return result ?? throw new InvalidOperationException("Yahoo Finance quoteSummary endpoint returned no result.");
    }

    private static CompanyProfile? ParseResultArray(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary endpoint returned no result.");
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary endpoint returned no result.");
        }

        var result = ParseResultObject(ref reader);
        SkipRemainingArrayValues(ref reader);
        return result;
    }

    private static CompanyProfile ParseResultObject(ref Utf8JsonReader reader)
    {
        string? symbol = null;
        var price = default(PriceFields);
        var quoteType = default(QuoteTypeFields);
        var summary = default(SummaryDetailFields);
        var asset = default(AssetProfileFields);
        var financial = default(FinancialDataFields);
        var stats = default(DefaultKeyStatisticsFields);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance company profile response did not contain a symbol.");
            }

            var isSymbol = reader.ValueTextEquals("symbol"u8);
            var isPrice = reader.ValueTextEquals("price"u8);
            var isQuoteType = reader.ValueTextEquals("quoteType"u8);
            var isSummaryDetail = reader.ValueTextEquals("summaryDetail"u8);
            var isAssetProfile = reader.ValueTextEquals("assetProfile"u8);
            var isFinancialData = reader.ValueTextEquals("financialData"u8);
            var isDefaultKeyStatistics = reader.ValueTextEquals("defaultKeyStatistics"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isSymbol)
            {
                symbol = ReadNullableString(ref reader);
            }
            else if (isPrice && reader.TokenType == JsonTokenType.StartObject)
            {
                price = ParsePriceObject(ref reader);
            }
            else if (isQuoteType && reader.TokenType == JsonTokenType.StartObject)
            {
                quoteType = ParseQuoteTypeObject(ref reader);
            }
            else if (isSummaryDetail && reader.TokenType == JsonTokenType.StartObject)
            {
                summary = ParseSummaryDetailObject(ref reader);
            }
            else if (isAssetProfile && reader.TokenType == JsonTokenType.StartObject)
            {
                asset = ParseAssetProfileObject(ref reader);
            }
            else if (isFinancialData && reader.TokenType == JsonTokenType.StartObject)
            {
                financial = ParseFinancialDataObject(ref reader);
            }
            else if (isDefaultKeyStatistics && reader.TokenType == JsonTokenType.StartObject)
            {
                stats = ParseDefaultKeyStatisticsObject(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        symbol = price.Symbol ?? quoteType.Symbol ?? symbol;
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new InvalidOperationException("Yahoo Finance company profile response did not contain a symbol.");
        }

        return new CompanyProfile(
            symbol,
            price.ShortName,
            price.LongName,
            quoteType.QuoteType,
            price.ExchangeName ?? price.Exchange,
            price.Currency,
            price.MarketCap ?? summary.MarketCap ?? summary.TotalAssets ?? stats.TotalAssets,
            stats.EnterpriseValue,
            summary.TrailingPe,
            summary.ForwardPe ?? stats.ForwardPe,
            summary.DividendYield ?? summary.Yield ?? stats.Yield,
            summary.Beta ?? stats.Beta,
            financial.CurrentPrice ?? price.RegularMarketPrice,
            financial.EarningsGrowth,
            financial.RevenueGrowth,
            financial.ProfitMargins,
            financial.GrossMargins,
            financial.OperatingMargins,
            financial.ReturnOnAssets,
            financial.ReturnOnEquity,
            asset.Sector ?? asset.SectorDisplay,
            asset.Industry ?? asset.IndustryDisplay,
            asset.Website,
            asset.InvestorRelationsWebsite,
            asset.Phone,
            asset.AddressLine1,
            asset.City,
            asset.State,
            asset.PostalCode,
            asset.Country,
            asset.LongBusinessSummary,
            asset.FullTimeEmployees,
            asset.LogoUrl ?? price.LogoUrl,
            asset.Officers ?? []);
    }

    private static PriceFields ParsePriceObject(ref Utf8JsonReader reader)
    {
        var result = default(PriceFields);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance company profile response contained an invalid price object.");
            }

            var isSymbol = reader.ValueTextEquals("symbol"u8);
            var isShortName = reader.ValueTextEquals("shortName"u8);
            var isLongName = reader.ValueTextEquals("longName"u8);
            var isExchangeName = reader.ValueTextEquals("exchangeName"u8);
            var isExchange = reader.ValueTextEquals("exchange"u8);
            var isCurrency = reader.ValueTextEquals("currency"u8);
            var isMarketCap = reader.ValueTextEquals("marketCap"u8);
            var isRegularMarketPrice = reader.ValueTextEquals("regularMarketPrice"u8);
            var isLogoUrl = reader.ValueTextEquals("logoUrl"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isSymbol) result.Symbol = ReadNullableString(ref reader);
            else if (isShortName) result.ShortName = ReadNullableString(ref reader);
            else if (isLongName) result.LongName = ReadNullableString(ref reader);
            else if (isExchangeName) result.ExchangeName = ReadNullableString(ref reader);
            else if (isExchange) result.Exchange = ReadNullableString(ref reader);
            else if (isCurrency) result.Currency = ReadNullableString(ref reader);
            else if (isMarketCap) result.MarketCap = ReadNullableDecimal(ref reader);
            else if (isRegularMarketPrice) result.RegularMarketPrice = ReadNullableDecimal(ref reader);
            else if (isLogoUrl) result.LogoUrl = ReadNullableString(ref reader);
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }

        return result;
    }

    private static QuoteTypeFields ParseQuoteTypeObject(ref Utf8JsonReader reader)
    {
        var result = default(QuoteTypeFields);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new InvalidOperationException("Yahoo Finance company profile response contained an invalid quoteType object.");

            var isSymbol = reader.ValueTextEquals("symbol"u8);
            var isQuoteType = reader.ValueTextEquals("quoteType"u8);
            if (!reader.Read()) break;
            if (isSymbol) result.Symbol = ReadNullableString(ref reader);
            else if (isQuoteType) result.QuoteType = ReadNullableString(ref reader);
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }

        return result;
    }

    private static SummaryDetailFields ParseSummaryDetailObject(ref Utf8JsonReader reader)
    {
        var result = default(SummaryDetailFields);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new InvalidOperationException("Yahoo Finance company profile response contained an invalid summaryDetail object.");

            var isMarketCap = reader.ValueTextEquals("marketCap"u8);
            var isTrailingPe = reader.ValueTextEquals("trailingPE"u8);
            var isForwardPe = reader.ValueTextEquals("forwardPE"u8);
            var isDividendYield = reader.ValueTextEquals("dividendYield"u8);
            var isBeta = reader.ValueTextEquals("beta"u8);
            var isTotalAssets = reader.ValueTextEquals("totalAssets"u8);
            var isYield = reader.ValueTextEquals("yield"u8);
            if (!reader.Read()) break;
            if (isMarketCap) result.MarketCap = ReadNullableDecimal(ref reader);
            else if (isTrailingPe) result.TrailingPe = ReadNullableDecimal(ref reader);
            else if (isForwardPe) result.ForwardPe = ReadNullableDecimal(ref reader);
            else if (isDividendYield) result.DividendYield = ReadNullableDecimal(ref reader);
            else if (isBeta) result.Beta = ReadNullableDecimal(ref reader);
            else if (isTotalAssets) result.TotalAssets = ReadNullableDecimal(ref reader);
            else if (isYield) result.Yield = ReadNullableDecimal(ref reader);
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }

        return result;
    }

    private static AssetProfileFields ParseAssetProfileObject(ref Utf8JsonReader reader)
    {
        var result = default(AssetProfileFields);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new InvalidOperationException("Yahoo Finance company profile response contained an invalid assetProfile object.");

            var isSector = reader.ValueTextEquals("sector"u8);
            var isSectorDisp = reader.ValueTextEquals("sectorDisp"u8);
            var isIndustry = reader.ValueTextEquals("industry"u8);
            var isIndustryDisp = reader.ValueTextEquals("industryDisp"u8);
            var isWebsite = reader.ValueTextEquals("website"u8);
            var isIrWebsite = reader.ValueTextEquals("irWebsite"u8);
            var isPhone = reader.ValueTextEquals("phone"u8);
            var isAddress1 = reader.ValueTextEquals("address1"u8);
            var isCity = reader.ValueTextEquals("city"u8);
            var isState = reader.ValueTextEquals("state"u8);
            var isZip = reader.ValueTextEquals("zip"u8);
            var isCountry = reader.ValueTextEquals("country"u8);
            var isLongBusinessSummary = reader.ValueTextEquals("longBusinessSummary"u8);
            var isFullTimeEmployees = reader.ValueTextEquals("fullTimeEmployees"u8);
            var isLogoUrl = reader.ValueTextEquals("logoUrl"u8);
            var isCompanyOfficers = reader.ValueTextEquals("companyOfficers"u8);
            if (!reader.Read()) break;
            if (isSector) result.Sector = ReadNullableString(ref reader);
            else if (isSectorDisp) result.SectorDisplay = ReadNullableString(ref reader);
            else if (isIndustry) result.Industry = ReadNullableString(ref reader);
            else if (isIndustryDisp) result.IndustryDisplay = ReadNullableString(ref reader);
            else if (isWebsite) result.Website = ReadNullableString(ref reader);
            else if (isIrWebsite) result.InvestorRelationsWebsite = ReadNullableString(ref reader);
            else if (isPhone) result.Phone = ReadNullableString(ref reader);
            else if (isAddress1) result.AddressLine1 = ReadNullableString(ref reader);
            else if (isCity) result.City = ReadNullableString(ref reader);
            else if (isState) result.State = ReadNullableString(ref reader);
            else if (isZip) result.PostalCode = ReadNullableString(ref reader);
            else if (isCountry) result.Country = ReadNullableString(ref reader);
            else if (isLongBusinessSummary) result.LongBusinessSummary = ReadNullableString(ref reader);
            else if (isFullTimeEmployees) result.FullTimeEmployees = ReadNullableInt32(ref reader);
            else if (isLogoUrl) result.LogoUrl = ReadNullableString(ref reader);
            else if (isCompanyOfficers) result.Officers = reader.TokenType == JsonTokenType.StartArray ? ParseOfficersArray(ref reader) : [];
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }

        return result;
    }

    private static FinancialDataFields ParseFinancialDataObject(ref Utf8JsonReader reader)
    {
        var result = default(FinancialDataFields);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new InvalidOperationException("Yahoo Finance company profile response contained an invalid financialData object.");

            var isCurrentPrice = reader.ValueTextEquals("currentPrice"u8);
            var isEarningsGrowth = reader.ValueTextEquals("earningsGrowth"u8);
            var isRevenueGrowth = reader.ValueTextEquals("revenueGrowth"u8);
            var isProfitMargins = reader.ValueTextEquals("profitMargins"u8);
            var isGrossMargins = reader.ValueTextEquals("grossMargins"u8);
            var isOperatingMargins = reader.ValueTextEquals("operatingMargins"u8);
            var isReturnOnAssets = reader.ValueTextEquals("returnOnAssets"u8);
            var isReturnOnEquity = reader.ValueTextEquals("returnOnEquity"u8);
            if (!reader.Read()) break;
            if (isCurrentPrice) result.CurrentPrice = ReadNullableDecimal(ref reader);
            else if (isEarningsGrowth) result.EarningsGrowth = ReadNullableDecimal(ref reader);
            else if (isRevenueGrowth) result.RevenueGrowth = ReadNullableDecimal(ref reader);
            else if (isProfitMargins) result.ProfitMargins = ReadNullableDecimal(ref reader);
            else if (isGrossMargins) result.GrossMargins = ReadNullableDecimal(ref reader);
            else if (isOperatingMargins) result.OperatingMargins = ReadNullableDecimal(ref reader);
            else if (isReturnOnAssets) result.ReturnOnAssets = ReadNullableDecimal(ref reader);
            else if (isReturnOnEquity) result.ReturnOnEquity = ReadNullableDecimal(ref reader);
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }

        return result;
    }

    private static DefaultKeyStatisticsFields ParseDefaultKeyStatisticsObject(ref Utf8JsonReader reader)
    {
        var result = default(DefaultKeyStatisticsFields);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new InvalidOperationException("Yahoo Finance company profile response contained an invalid defaultKeyStatistics object.");

            var isEnterpriseValue = reader.ValueTextEquals("enterpriseValue"u8);
            var isForwardPe = reader.ValueTextEquals("forwardPE"u8);
            var isBeta = reader.ValueTextEquals("beta"u8);
            var isTotalAssets = reader.ValueTextEquals("totalAssets"u8);
            var isYield = reader.ValueTextEquals("yield"u8);
            if (!reader.Read()) break;
            if (isEnterpriseValue) result.EnterpriseValue = ReadNullableDecimal(ref reader);
            else if (isForwardPe) result.ForwardPe = ReadNullableDecimal(ref reader);
            else if (isBeta) result.Beta = ReadNullableDecimal(ref reader);
            else if (isTotalAssets) result.TotalAssets = ReadNullableDecimal(ref reader);
            else if (isYield) result.Yield = ReadNullableDecimal(ref reader);
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }

        return result;
    }

    private static CompanyOfficer[] ParseOfficersArray(ref Utf8JsonReader reader)
    {
        var items = new List<CompanyOfficer>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
                continue;
            }
            items.Add(ParseOfficerObject(ref reader));
        }
        return items.ToArray();
    }

    private static CompanyOfficer ParseOfficerObject(ref Utf8JsonReader reader)
    {
        string? name = null;
        string? title = null;
        int? age = null;
        int? yearBorn = null;
        decimal? totalPay = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new InvalidOperationException("Yahoo Finance company profile response contained an invalid company officer object.");

            var isName = reader.ValueTextEquals("name"u8);
            var isTitle = reader.ValueTextEquals("title"u8);
            var isAge = reader.ValueTextEquals("age"u8);
            var isYearBorn = reader.ValueTextEquals("yearBorn"u8);
            var isTotalPay = reader.ValueTextEquals("totalPay"u8);
            if (!reader.Read()) break;
            if (isName) name = ReadNullableString(ref reader);
            else if (isTitle) title = ReadNullableString(ref reader);
            else if (isAge) age = ReadNullableInt32(ref reader);
            else if (isYearBorn) yearBorn = ReadNullableInt32(ref reader);
            else if (isTotalPay) totalPay = ReadNullableDecimal(ref reader);
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }

        return new CompanyOfficer(name, title, age, yearBorn, totalPay);
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

    private static string? ReadRawWrappedString(ref Utf8JsonReader reader)
    {
        string? result = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new InvalidOperationException("Yahoo Finance company profile response contained an invalid wrapped value.");
            var isRaw = reader.ValueTextEquals("raw"u8);
            if (!reader.Read()) break;
            if (isRaw) result = ReadNullableString(ref reader);
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }
        return result;
    }

    private static decimal? ReadRawWrappedDecimal(ref Utf8JsonReader reader)
    {
        decimal? result = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new InvalidOperationException("Yahoo Finance company profile response contained an invalid wrapped value.");
            var isRaw = reader.ValueTextEquals("raw"u8);
            if (!reader.Read()) break;
            if (isRaw) result = ReadNullableDecimal(ref reader);
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }
        return result;
    }

    private static int? ReadRawWrappedInt32(ref Utf8JsonReader reader)
    {
        int? result = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new InvalidOperationException("Yahoo Finance company profile response contained an invalid wrapped value.");
            var isRaw = reader.ValueTextEquals("raw"u8);
            if (!reader.Read()) break;
            if (isRaw) result = ReadNullableInt32(ref reader);
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
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

    private static void SkipPropertyValue(ref Utf8JsonReader reader)
    {
        if (!reader.Read()) return;
        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray or JsonTokenType.PropertyName) reader.Skip();
    }

    private static void SkipRemainingArrayValues(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) return;
            reader.Skip();
        }
    }

    private struct PriceFields
    {
        public string? Symbol;
        public string? ShortName;
        public string? LongName;
        public string? ExchangeName;
        public string? Exchange;
        public string? Currency;
        public decimal? MarketCap;
        public decimal? RegularMarketPrice;
        public string? LogoUrl;
    }

    private struct QuoteTypeFields
    {
        public string? Symbol;
        public string? QuoteType;
    }

    private struct SummaryDetailFields
    {
        public decimal? MarketCap;
        public decimal? TrailingPe;
        public decimal? ForwardPe;
        public decimal? DividendYield;
        public decimal? Beta;
        public decimal? TotalAssets;
        public decimal? Yield;
    }

    private struct AssetProfileFields
    {
        public string? Sector;
        public string? SectorDisplay;
        public string? Industry;
        public string? IndustryDisplay;
        public string? Website;
        public string? InvestorRelationsWebsite;
        public string? Phone;
        public string? AddressLine1;
        public string? City;
        public string? State;
        public string? PostalCode;
        public string? Country;
        public string? LongBusinessSummary;
        public int? FullTimeEmployees;
        public string? LogoUrl;
        public CompanyOfficer[] Officers;
    }

    private struct FinancialDataFields
    {
        public decimal? CurrentPrice;
        public decimal? EarningsGrowth;
        public decimal? RevenueGrowth;
        public decimal? ProfitMargins;
        public decimal? GrossMargins;
        public decimal? OperatingMargins;
        public decimal? ReturnOnAssets;
        public decimal? ReturnOnEquity;
    }

    private struct DefaultKeyStatisticsFields
    {
        public decimal? EnterpriseValue;
        public decimal? ForwardPe;
        public decimal? Beta;
        public decimal? TotalAssets;
        public decimal? Yield;
    }
}