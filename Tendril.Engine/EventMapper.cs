using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Abstractions;

namespace Tendril.Engine;

public class EventMapper : IEventMapper
{
    private static readonly Dictionary<string, System.Reflection.PropertyInfo> _eventProperties =
        typeof(Event).GetProperties()
        .Where(p => p.CanWrite)
        .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    public Event Map(ScraperDefinition scraper, ScrapedEventRaw raw)
    {
        if (scraper.VenueId is null)
            throw new InvalidOperationException("Scraper must be associated with a Venue before mapping events.");

        var mappedEvent = new Event
        {
            Id = Guid.NewGuid(),
            ScraperDefinitionId = scraper.Id,
            VenueId = scraper.VenueId.Value,
            ScrapedAtUtc = raw.ScrapedAtUtc,
            Title = "(unmapped)"
        };

        var doc = JsonDocument.Parse(raw.RawDataJson);
        var root = doc.RootElement;

        if (!TryGetValue(root, "Fields", out var fields))
        {
            return mappedEvent;
        }

        var scratchpad = new Dictionary<string, object?>();

        foreach (var rule in scraper.MappingRules.OrderBy(x => x.Order))
        {
            ApplyRule(fields, rule, scratchpad);
        }

        foreach (var (targetField, value) in scratchpad)
        {
            AssignField(mappedEvent, targetField, value);
        }

        return mappedEvent;
    }
    private static void ApplyRule(JsonElement raw, ScraperMappingRule rule, Dictionary<string, object?> scratch)
    {
        object? primary;

        // 1. Check Scratchpad first (precedence)
        if (scratch.TryGetValue(rule.SourceField, out var scratchVal))
        {
            primary = scratchVal;
        }
        // 2. Fallback to Raw JSON
        else if (TryGetValue(raw, rule.SourceField, out var rawVal))
        {
            primary = rawVal;
        }
        // 3. If neither, skip
        else
        {
            return;
        }

        object? secondary = null;

        if (!string.IsNullOrWhiteSpace(rule.CombineWithField))
        {
            // Same precedence logic for the secondary field
            if (scratch.TryGetValue(rule.CombineWithField!, out var combinedScratch))
            {
                secondary = combinedScratch;
            }
            else if (TryGetValue(raw, rule.CombineWithField!, out var combinedRaw))
            {
                secondary = combinedRaw;
            }
        }

        // Transform
        var value = ApplyTransform(
            rule.TransformType,
            primary,
            secondary,
            rule.Format,
            rule.RegexPattern,
            rule.RegexReplacement,
            rule.SplitDelimiter);

        scratch[rule.TargetField] = value;
    }

    private static void AssignField(Event evt, string targetField, object? value)
    {
        if (value is null) return;

        if (!_eventProperties.TryGetValue(targetField, out var prop))
            return;

        if (prop.PropertyType == typeof(string))
        {
            prop.SetValue(evt, value.ToString());
            return;
        }

        if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
        {
            try
            {
                var d = Convert.ToDecimal(value);
                prop.SetValue(evt, d);
            }
            catch { }

            return;
        }
        if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
        {
            try
            {
                var i = Convert.ToInt32(value);
                prop.SetValue(evt, i);
            }
            catch { }

            return;
        }
        else if (prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTimeOffset?))
        {
            if (value is DateTimeOffset dto)
                prop.SetValue(evt, dto);
        }
        else
        {
            prop.SetValue(evt, value);
        }
    }

    private static object? ApplyTransform(
        TransformType transform,
        object? primary,
        object? secondary,
        string? dateFormat = null,
        string? regexPattern = null,
        string? regexReplacement = null,
        string? splitDelimiter = null)
    {
        // FIX: If no transform is needed, return the raw object to preserve its type
        // (This keeps DateTimeOffset as DateTimeOffset, etc.)
        if (transform == TransformType.None && secondary is null)
        {
            // If it's a JsonElement, we still might want to unbox it to a string/number
            if (primary is JsonElement)
            {
                return GetString(primary);
            }

            return primary;
        }

        var primaryVal = GetString(primary);
        var secondaryVal = GetString(secondary);

        switch (transform)
        {
            //case TransformType.None:
            //{
            //    return primaryVal;
            //}

            case TransformType.Trim:
            {
                return primaryVal?.Trim();
            }

            case TransformType.ToLower:
            {
                return primaryVal?.ToLowerInvariant();
            }

            case TransformType.ToUpper:
            {
                return primaryVal?.ToUpperInvariant();
            }

            case TransformType.Split:
            {
                if (string.IsNullOrWhiteSpace(primaryVal) || string.IsNullOrWhiteSpace(splitDelimiter))
                {
                    return primaryVal;
                }

                return primaryVal
                    .Split(splitDelimiter, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();
            }

            case TransformType.Combine:
            {
                if (primary is DateTimeOffset datePart)
                {
                    if (secondary is DateTimeOffset timePart)
                    {
                        return new DateTimeOffset(
                            datePart.Year, datePart.Month, datePart.Day,
                            timePart.Hour, timePart.Minute, timePart.Second,
                            datePart.Offset);
                    }

                    return datePart;
                }

                return (primaryVal, secondaryVal) switch
                {
                    (string p, string s) => $"{p} {s}",
                    (string p, null) => p,
                    (null, string s) => s,
                    _ => null
                };
            }

            case TransformType.RegexExtract:
            {
                if (string.IsNullOrWhiteSpace(primaryVal) || string.IsNullOrWhiteSpace(regexPattern))
                {
                    return primaryVal;
                }

                var match = Regex.Match(primaryVal, regexPattern, RegexOptions.Singleline);

                return match.Success ? match.Value : null;
            }

            case TransformType.RegexReplace:
            {
                if (string.IsNullOrWhiteSpace(primaryVal) ||
                    string.IsNullOrWhiteSpace(regexPattern) ||
                    regexReplacement is null)
                {
                    return primaryVal;
                }

                return Regex.Replace(primaryVal, regexPattern, regexReplacement);
            }

            case TransformType.ParseDate:
            {
                if (DateTimeOffset.TryParse(primaryVal, out var dateOnly))
                {
                    if (dateOnly < DateTimeOffset.UtcNow.AddMonths(-3))
                    {
                        dateOnly = dateOnly.AddYears(1);
                    }

                    return dateOnly;
                }

                return null;
            }

            case TransformType.ParseTime:
            {
                // Parse ONLY the time portion, combine with today if needed
                if (string.IsNullOrWhiteSpace(primaryVal))
                {
                    return null;
                }

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
            }

            case TransformType.ParseExact:
            {
                if (primaryVal is null || dateFormat is null)
                    return null;

                if (DateTimeOffset.TryParseExact(
                    primaryVal,
                    dateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsed))
                {
                    return parsed;
                }

                return null;
            }

            // TODO: hack for now
            case TransformType.ParseLoose:
            {
                if (string.IsNullOrWhiteSpace(primaryVal))
                    return null;

                // Remove weekday names
                var cleaned = Regex.Replace(primaryVal, @"^(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday),\s*", "", RegexOptions.IgnoreCase);

                // Remove "@"
                cleaned = cleaned.Replace("@", "", StringComparison.OrdinalIgnoreCase);

                // Normalize spacing and casing
                cleaned = cleaned
                    .Replace("pm", " PM", StringComparison.OrdinalIgnoreCase)
                    .Replace("am", " AM", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                if (DateTime.TryParse(cleaned, out var dt))
                    return new DateTimeOffset(dt);

                return null;
            }

            case TransformType.Currency:
            {
                if (string.IsNullOrWhiteSpace(primaryVal))
                    return null;

                var cleaned = new string(primaryVal.Where(c =>
                    char.IsDigit(c) || c == '.' || c == '-').ToArray());

                return decimal.TryParse(cleaned, out var money)
                    ? money
                    : null;
            }

            case TransformType.SrcSetToUrl:
            {
                return ExtractBestImageFromSrcSet(primaryVal);
            }

            default:
            {
                // Safe fallback
                return primaryVal;
            }
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

    private static string? GetString(object? item)
    {
        if (item is null) return null;

        // If it came from the raw JSON, unbox and parse it
        if (item is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.ToString(),
                JsonValueKind.Null => null,
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Array => string.Join(", ", element.EnumerateArray().Select(e => GetString(e))),
                JsonValueKind.Object => element.ToString(),
                _ => null
            };
        }

        // If it came from the scratchpad (String, DateTime, Decimal, etc.)
        return item.ToString();
    }

    private static string? ExtractBestImageFromSrcSet(string? srcSet)
    {
        if (string.IsNullOrWhiteSpace(srcSet)) return null;

        // 1. Split the srcset by comma to get the list of variants
        //    Format: "url1 100w, url2 200w, url3 500w"
        var variants = srcSet.Split(',');

        // 2. Get the last variant. 
        //    Conventionally, srcset lists are ordered by size, so the last one is the largest.
        var bestVariant = variants.Last().Trim();

        // 3. Isolate the URL from the width descriptor (remove the " 1319w" part)
        var rawUrl = bestVariant.Split(' ')[0];

        // 4. Decode Next.js / Proxy URLs
        //    Pattern looks for ?url=... or &url=...
        if (rawUrl.Contains("url="))
        {
            // We use Regex here to avoid adding a dependency on System.Web
            var match = Regex.Match(rawUrl, @"[?&]url=([^&]+)");
            if (match.Success)
            {
                // Decode the URL (e.g. https%3A%2F%2F... -> https://...)
                // System.Net.WebUtility is standard in .NET Core+
                return System.Net.WebUtility.UrlDecode(match.Groups[1].Value);
            }
        }

        return rawUrl;
    }
}
