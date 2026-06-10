using System.Buffers;
using System.Text;
using System.Text.Json;

namespace YFinance.Net;

internal static class SearchResponseParser
{
    public static SearchResult Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Parse(Encoding.UTF8.GetBytes(json));
    }

    public static SearchResult Parse(ReadOnlyMemory<byte> json)
    {
        var reader = new Utf8JsonReader(json.Span, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance search response did not contain a root object.");
        }

        var quotes = Array.Empty<SearchQuote>();
        var news = Array.Empty<SearchNewsItem>();
        var listCount = 0;
        var researchReportCount = 0;
        var navigationLinkCount = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance search response did not contain a root object.");
            }

            var isQuotes = reader.ValueTextEquals("quotes"u8);
            var isNews = reader.ValueTextEquals("news"u8);
            var isLists = reader.ValueTextEquals("lists"u8);
            var isResearchReports = reader.ValueTextEquals("researchReports"u8);
            var isNav = reader.ValueTextEquals("nav"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isQuotes && reader.TokenType == JsonTokenType.StartArray)
            {
                quotes = ParseQuotesArray(ref reader);
            }
            else if (isNews && reader.TokenType == JsonTokenType.StartArray)
            {
                news = ParseNewsArray(ref reader);
            }
            else if (isLists && reader.TokenType == JsonTokenType.StartArray)
            {
                listCount = CountArrayItems(ref reader);
            }
            else if (isResearchReports && reader.TokenType == JsonTokenType.StartArray)
            {
                researchReportCount = CountArrayItems(ref reader);
            }
            else if (isNav && reader.TokenType == JsonTokenType.StartArray)
            {
                navigationLinkCount = CountArrayItems(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return new SearchResult(quotes, news, listCount, researchReportCount, navigationLinkCount);
    }

    private static SearchQuote[] ParseQuotesArray(ref Utf8JsonReader reader)
    {
        var quotes = new List<SearchQuote>();
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

            var symbol = default(string);
            var shortName = default(string);
            var longName = default(string);
            var quoteType = default(string);
            var exchange = default(string);
            var exchangeDisplayName = default(string);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new InvalidOperationException("Yahoo Finance search quotes array contained an invalid quote item.");
                }

                var isSymbol = reader.ValueTextEquals("symbol"u8);
                var isShortName = reader.ValueTextEquals("shortname"u8);
                var isLongName = reader.ValueTextEquals("longname"u8);
                var isQuoteType = reader.ValueTextEquals("quoteType"u8);
                var isExchange = reader.ValueTextEquals("exchange"u8);
                var isExchangeDisplayName = reader.ValueTextEquals("exchDisp"u8);

                if (!reader.Read())
                {
                    break;
                }

                if (isSymbol)
                {
                    symbol = ReadNullableString(ref reader);
                }
                else if (isShortName)
                {
                    shortName = ReadNullableString(ref reader);
                }
                else if (isLongName)
                {
                    longName = ReadNullableString(ref reader);
                }
                else if (isQuoteType)
                {
                    quoteType = ReadNullableString(ref reader);
                }
                else if (isExchange)
                {
                    exchange = ReadNullableString(ref reader);
                }
                else if (isExchangeDisplayName)
                {
                    exchangeDisplayName = ReadNullableString(ref reader);
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

            quotes.Add(new SearchQuote(symbol, shortName, longName, quoteType, exchange, exchangeDisplayName));
        }

        return quotes.ToArray();
    }

    private static SearchNewsItem[] ParseNewsArray(ref Utf8JsonReader reader)
    {
        var newsItems = new List<SearchNewsItem>();
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

            string? id = null;
            string? title = null;
            string? publisher = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new InvalidOperationException("Yahoo Finance search news array contained an invalid news item.");
                }

                var isId = reader.ValueTextEquals("uuid"u8);
                var isTitle = reader.ValueTextEquals("title"u8);
                var isPublisher = reader.ValueTextEquals("publisher"u8);

                if (!reader.Read())
                {
                    break;
                }

                if (isId)
                {
                    id = ReadNullableString(ref reader);
                }
                else if (isTitle)
                {
                    title = ReadNullableString(ref reader);
                }
                else if (isPublisher)
                {
                    publisher = ReadNullableString(ref reader);
                }
                else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }
            }

            newsItems.Add(new SearchNewsItem(id, title, publisher));
        }

        return newsItems.ToArray();
    }

    private static int CountArrayItems(ref Utf8JsonReader reader)
    {
        var count = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            count++;
            if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
            {
                reader.Skip();
            }
        }

        return count;
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

    private static string ReadScalarText(ref Utf8JsonReader reader)
    {
        if (reader.HasValueSequence)
        {
            return Encoding.UTF8.GetString(reader.ValueSequence.ToArray());
        }

        return Encoding.UTF8.GetString(reader.ValueSpan);
    }
}