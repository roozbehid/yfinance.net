using System.Buffers;
using System.Text;
using System.Text.Json;

namespace YFinance.Net;

internal static class TickerNewsResponseParser
{
    public static TickerNewsItem[] Parse(ReadOnlyMemory<byte> json)
    {
        var reader = new Utf8JsonReader(json.Span, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance ticker news response did not contain a root object.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance ticker news response did not contain a root object.");
            }

            var isData = reader.ValueTextEquals("data"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isData && reader.TokenType == JsonTokenType.StartObject)
            {
                return ParseDataObject(ref reader);
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return Array.Empty<TickerNewsItem>();
    }

    private static TickerNewsItem[] ParseDataObject(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance ticker news response contained an invalid data object.");
            }

            var isTickerStream = reader.ValueTextEquals("tickerStream"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isTickerStream && reader.TokenType == JsonTokenType.StartObject)
            {
                return ParseTickerStreamObject(ref reader);
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return Array.Empty<TickerNewsItem>();
    }

    private static TickerNewsItem[] ParseTickerStreamObject(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance ticker news response contained an invalid tickerStream object.");
            }

            var isStream = reader.ValueTextEquals("stream"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isStream && reader.TokenType == JsonTokenType.StartArray)
            {
                return ParseStreamArray(ref reader);
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return Array.Empty<TickerNewsItem>();
    }

    private static TickerNewsItem[] ParseStreamArray(ref Utf8JsonReader reader)
    {
        var items = new List<TickerNewsItem>();
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

            if (TryParseStreamItem(ref reader, out var item))
            {
                items.Add(item);
            }
        }

        return items.ToArray();
    }

    private static bool TryParseStreamItem(ref Utf8JsonReader reader, out TickerNewsItem item)
    {
        string? itemId = null;
        ParsedTickerNewsContent content = default;
        var isAd = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance ticker news stream contained an invalid item.");
            }

            var isId = reader.ValueTextEquals("id"u8);
            var isContent = reader.ValueTextEquals("content"u8);
            var isAdProperty = reader.ValueTextEquals("ad"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isId)
            {
                itemId = ReadNullableString(ref reader);
            }
            else if (isContent && reader.TokenType == JsonTokenType.StartObject)
            {
                content = ParseContentObject(ref reader);
            }
            else if (isAdProperty)
            {
                isAd = reader.TokenType != JsonTokenType.Null;
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        if (isAd || string.IsNullOrWhiteSpace(content.Title))
        {
            item = default;
            return false;
        }

        item = new TickerNewsItem(
            content.Id ?? itemId,
            content.ContentType,
            content.Title,
            content.Summary,
            content.PublishedAt,
            content.Provider,
            content.CanonicalUrl,
            content.ClickThroughUrl,
            content.ThumbnailUrl,
            content.IsHosted,
            content.EditorsPick);
        return true;
    }

    private static ParsedTickerNewsContent ParseContentObject(ref Utf8JsonReader reader)
    {
        string? id = null;
        string? contentType = null;
        string? title = null;
        string? summary = null;
        DateTimeOffset? publishedAt = null;
        string? provider = null;
        string? canonicalUrl = null;
        string? clickThroughUrl = null;
        string? thumbnailUrl = null;
        var isHosted = false;
        var editorsPick = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance ticker news content object was invalid.");
            }

            var isId = reader.ValueTextEquals("id"u8);
            var isContentType = reader.ValueTextEquals("contentType"u8);
            var isTitle = reader.ValueTextEquals("title"u8);
            var isSummary = reader.ValueTextEquals("summary"u8);
            var isPubDate = reader.ValueTextEquals("pubDate"u8);
            var isIsHosted = reader.ValueTextEquals("isHosted"u8);
            var isProvider = reader.ValueTextEquals("provider"u8);
            var isCanonicalUrl = reader.ValueTextEquals("canonicalUrl"u8);
            var isClickThroughUrl = reader.ValueTextEquals("clickThroughUrl"u8);
            var isThumbnail = reader.ValueTextEquals("thumbnail"u8);
            var isMetadata = reader.ValueTextEquals("metadata"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isId)
            {
                id = ReadNullableString(ref reader);
            }
            else if (isContentType)
            {
                contentType = ReadNullableString(ref reader);
            }
            else if (isTitle)
            {
                title = ReadNullableString(ref reader);
            }
            else if (isSummary)
            {
                summary = ReadNullableString(ref reader);
            }
            else if (isPubDate)
            {
                publishedAt = ReadNullableDateTimeOffset(ref reader);
            }
            else if (isIsHosted)
            {
                isHosted = ReadBoolean(ref reader);
            }
            else if (isProvider && reader.TokenType == JsonTokenType.StartObject)
            {
                provider = ParseDisplayNameObject(ref reader);
            }
            else if (isCanonicalUrl && reader.TokenType == JsonTokenType.StartObject)
            {
                canonicalUrl = ParseUrlObject(ref reader);
            }
            else if (isClickThroughUrl && reader.TokenType == JsonTokenType.StartObject)
            {
                clickThroughUrl = ParseUrlObject(ref reader);
            }
            else if (isThumbnail && reader.TokenType == JsonTokenType.StartObject)
            {
                thumbnailUrl = ParseThumbnailObject(ref reader);
            }
            else if (isMetadata && reader.TokenType == JsonTokenType.StartObject)
            {
                editorsPick = ParseEditorsPickObject(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return new ParsedTickerNewsContent(
            id,
            contentType,
            title,
            summary,
            publishedAt,
            provider,
            canonicalUrl,
            clickThroughUrl,
            thumbnailUrl,
            isHosted,
            editorsPick);
    }

    private static string? ParseDisplayNameObject(ref Utf8JsonReader reader)
    {
        string? displayName = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance ticker news provider object was invalid.");
            }

            var isDisplayName = reader.ValueTextEquals("displayName"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isDisplayName)
            {
                displayName = ReadNullableString(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return displayName;
    }

    private static string? ParseUrlObject(ref Utf8JsonReader reader)
    {
        string? url = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance ticker news URL object was invalid.");
            }

            var isUrl = reader.ValueTextEquals("url"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isUrl)
            {
                url = ReadNullableString(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return url;
    }

    private static string? ParseThumbnailObject(ref Utf8JsonReader reader)
    {
        string? originalUrl = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance ticker news thumbnail object was invalid.");
            }

            var isOriginalUrl = reader.ValueTextEquals("originalUrl"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isOriginalUrl)
            {
                originalUrl = ReadNullableString(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return originalUrl;
    }

    private static bool ParseEditorsPickObject(ref Utf8JsonReader reader)
    {
        var editorsPick = false;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance ticker news metadata object was invalid.");
            }

            var isEditorsPick = reader.ValueTextEquals("editorsPick"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isEditorsPick)
            {
                editorsPick = ReadBoolean(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return editorsPick;
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

    private static DateTimeOffset? ReadNullableDateTimeOffset(ref Utf8JsonReader reader)
    {
        var value = ReadNullableString(ref reader);
        return DateTimeOffset.TryParse(value, out var timestamp) ? timestamp : null;
    }

    private static bool ReadBoolean(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String when bool.TryParse(reader.GetString(), out var value) => value,
            _ => false
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

    private readonly record struct ParsedTickerNewsContent(
        string? Id,
        string? ContentType,
        string? Title,
        string? Summary,
        DateTimeOffset? PublishedAt,
        string? Provider,
        string? CanonicalUrl,
        string? ClickThroughUrl,
        string? ThumbnailUrl,
        bool IsHosted,
        bool EditorsPick);
}