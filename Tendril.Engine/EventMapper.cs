using System.Text.Json;
using System.Text.RegularExpressions;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Abstractions;

namespace Tendril.Engine;

public class EventMapper : IEventMapper
{
    public Event Map(ScraperDefinition scraper, ScrapedEventRaw raw)
    {
        if (scraper.VenueId is null)
            throw new InvalidOperationException("Scraper must be associated with a Venue before mapping events.");

        var doc = JsonDocument.Parse(raw.RawDataJson);
        var root = doc.RootElement;

        var evt = new Event
        {
            Id = Guid.NewGuid(),
            VenueId = scraper.VenueId.Value,
            ScrapedAtUtc = raw.ScrapedAtUtc,
            Title = "(unmapped)" // will be overwritten by rules if configured
        };

        foreach (var rule in scraper.MappingRules)
        {
            ApplyRule(evt, rule, root);
        }

        return evt;
    }

    private void ApplyRule(Event evt, ScraperMappingRule rule, JsonElement root)
    {
        // Try to get source element
        if (!TryGetValue(root, rule.SourceField, out var primary))
            return;

        JsonElement? secondary = null;
        if (!string.IsNullOrWhiteSpace(rule.CombineWithField))
        {
            if (TryGetValue(root, rule.CombineWithField!, out var combined))
                secondary = combined;
        }

        var value = ApplyTransform(rule.TransformType, primary, secondary);

        // Assign to target property by name via reflection (simple & flexible)
        var prop = typeof(Event).GetProperty(rule.TargetField);
        if (prop == null || !prop.CanWrite)
            return;

        if (value is null)
            return;

        if (prop.PropertyType == typeof(string))
        {
            prop.SetValue(evt, value.ToString());
        }
        else if (prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTimeOffset?))
        {
            if (value is DateTimeOffset dto)
                prop.SetValue(evt, dto);
        }
        else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
        {
            if (value is decimal d)
                prop.SetValue(evt, d);
        }
        else
        {
            // You can expand this later (e.g., List<string>, etc.)
            prop.SetValue(evt, value);
        }
    }

    private static bool TryGetValue(JsonElement root, string fieldName, out JsonElement element)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty(fieldName, out element))
        {
            return true;
        }

        element = default;
        return false;
    }
    private object? ApplyTransform(
        TransformType transform,
        JsonElement primary,
        JsonElement? secondary,
        string? regexPattern = null,
        string? regexReplacement = null,
        string? splitDelimiter = null)
    {
        var primaryVal = GetString(primary);
        var secondaryVal = secondary.HasValue ? GetString(secondary.Value) : null;

        switch (transform)
        {
            case TransformType.None:
                return primaryVal;

            case TransformType.Trim:
                return primaryVal?.Trim();

            case TransformType.ToLower:
                return primaryVal?.ToLowerInvariant();

            case TransformType.ToUpper:
                return primaryVal?.ToUpperInvariant();

            case TransformType.Split:
                if (string.IsNullOrWhiteSpace(primaryVal) || string.IsNullOrWhiteSpace(splitDelimiter))
                    return primaryVal;

                return primaryVal.Split(splitDelimiter, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(x => x.Trim())
                                 .ToList();

            case TransformType.Combine:
                // Combine two strings (Title + Subtitle, Date + Time, etc.)
                if (string.IsNullOrEmpty(primaryVal) && string.IsNullOrEmpty(secondaryVal))
                    return null;

                return $"{primaryVal} {secondaryVal}".Trim();

            case TransformType.RegexExtract:
                if (string.IsNullOrWhiteSpace(primaryVal) || string.IsNullOrWhiteSpace(regexPattern))
                    return primaryVal;

                var match = Regex.Match(primaryVal, regexPattern, RegexOptions.Singleline);
                return match.Success ? match.Value : null;

            case TransformType.RegexReplace:
                if (string.IsNullOrWhiteSpace(primaryVal) ||
                    string.IsNullOrWhiteSpace(regexPattern) ||
                    regexReplacement is null)
                    return primaryVal;

                return Regex.Replace(primaryVal, regexPattern, regexReplacement);

            case TransformType.ParseDate:
                if (DateTimeOffset.TryParse(primaryVal, out var dateOnly))
                    return dateOnly;
                return null;

            case TransformType.ParseTime:
                // Parse ONLY the time portion, combine with today if needed
                if (string.IsNullOrWhiteSpace(primaryVal))
                    return null;

                if (DateTime.TryParse(primaryVal, out var timeOnly))
                {
                    var now = DateTimeOffset.UtcNow;
                    var combined = new DateTimeOffset(
                        now.Year, now.Month, now.Day,
                        timeOnly.Hour, timeOnly.Minute, timeOnly.Second,
                        now.Offset);
                    return combined;
                }
                return null;

            case TransformType.Currency:
                if (string.IsNullOrWhiteSpace(primaryVal))
                    return null;

                var cleaned = new string(primaryVal.Where(c =>
                    char.IsDigit(c) || c == '.' || c == '-').ToArray());

                return decimal.TryParse(cleaned, out var money)
                    ? money
                    : null;

            default:
                // Safe fallback
                return primaryVal;
        }
    }
    private static string? GetString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.Null => null,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",

            JsonValueKind.Array => string.Join(
                ", ",
                element.EnumerateArray().Select(e => GetString(e))
            ),

            JsonValueKind.Object => element.ToString(), // fallback

            _ => null
        };
    }

}
