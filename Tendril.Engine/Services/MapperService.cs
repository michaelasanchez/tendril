using System.Data;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Abstractions;

namespace Tendril.Engine.Services;

public class MapperService : IMapperService
{
    private static readonly Dictionary<string, System.Reflection.PropertyInfo> _eventProperties =
        typeof(Event).GetProperties()
        .Where(p => p.CanWrite)
        .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    public Event MapEvent(ScraperDefinition scraper, ScrapedEventRaw raw, int? referenceYear)
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

        var scratchpad = new Dictionary<string, object?> {
            {  "ReferenceYear", referenceYear }
        };

        foreach (var rule in scraper.MappingRules.OrderBy(x => x.Order))
        {
            ApplyRule(fields, rule, scratchpad, referenceYear);
        }

        foreach (var (targetField, value) in scratchpad)
        {
            AssignField(mappedEvent, targetField, value);
        }

        return mappedEvent;
    }

    private static void ApplyRule(JsonElement raw, ScraperMappingRule rule, Dictionary<string, object?> scratch, int? referenceYear)
    {
        object? primary = null;

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
        else if (rule.TransformType != TransformType.Constant)
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
            rule,
            primary,
            secondary,
            referenceYear);

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
        ScraperMappingRule rule,
        object? primary,
        object? secondary,
        int? referenceYear)
    {
        // FIX: If no transform is needed, return the raw object to preserve its type
        // (This keeps DateTimeOffset as DateTimeOffset, etc.)
        if (rule.TransformType == TransformType.None && secondary is null)
        {
            // If it's a JsonElement, we still might want to unbox it to a string/number
            if (primary is JsonElement)
            {
                return GetString(primary);
            }

            return primary;
        }

        var primaryInput = GetString(primary);
        var secondaryInput = GetString(secondary);

        switch (rule.TransformType)
        {
            case TransformType.Constant:
            {
                return rule.ConstantValue;
            }

            case TransformType.Trim:
            {
                return primaryInput?.Trim();
            }

            case TransformType.ToLower:
            {
                return primaryInput?.ToLowerInvariant();
            }

            case TransformType.ToUpper:
            {
                return primaryInput?.ToUpperInvariant();
            }

            case TransformType.Split:
            {
                if (string.IsNullOrWhiteSpace(primaryInput) || string.IsNullOrWhiteSpace(rule.SplitDelimiter))
                {
                    return primaryInput;
                }

                return primaryInput
                    .Split(rule.SplitDelimiter, StringSplitOptions.RemoveEmptyEntries)
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

                return (primaryInput, secondaryInput) switch
                {
                    (string p, string s) => $"{p} {s}",
                    (string p, null) => p,
                    (null, string s) => s,
                    _ => null
                };
            }

            case TransformType.RegexExtract:
            {
                if (string.IsNullOrWhiteSpace(primaryInput) || string.IsNullOrWhiteSpace(rule.RegexPattern))
                {
                    return primaryInput;
                }

                var match = Regex.Match(primaryInput, rule.RegexPattern, RegexOptions.Singleline);

                return match.Success ? match.Value : null;
            }

            case TransformType.RegexReplace:
            {
                if (string.IsNullOrWhiteSpace(primaryInput) ||
                    string.IsNullOrWhiteSpace(rule.RegexPattern) ||
                    rule.RegexReplacement is null)
                {
                    return primaryInput;
                }

                return Regex.Replace(primaryInput, rule.RegexPattern, rule.RegexReplacement);
            }

            case TransformType.ParseDate:
            {
                if (DateTimeOffset.TryParse(primaryInput, out var parsed))
                {
                    return new DateTimeOffset(
                        referenceYear ?? parsed.Year,
                        parsed.Month,
                        parsed.Day,
                        parsed.Hour,
                        parsed.Minute,
                        parsed.Second,
                        parsed.Offset);
                }

                return null;
            }

            case TransformType.ParseTime:
            {
                // Parse ONLY the time portion, combine with today if needed
                if (string.IsNullOrWhiteSpace(primaryInput))
                {
                    return null;
                }

                if (DateTime.TryParse(primaryInput, out var timeOnly))
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
                if (primaryInput is null || rule.Format is null)
                    return null;

                if (DateTimeOffset.TryParseExact(
                    primaryInput,
                    rule.Format,
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
                if (string.IsNullOrWhiteSpace(primaryInput))
                    return null;

                // Remove weekday names
                var cleaned = Regex.Replace(primaryInput, @"^(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday),\s*", "", RegexOptions.IgnoreCase);

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
                if (string.IsNullOrWhiteSpace(primaryInput))
                    return null;

                var cleaned = new string(primaryInput.Where(c =>
                    char.IsDigit(c) || c == '.' || c == '-').ToArray());

                return decimal.TryParse(cleaned, out var money)
                    ? money
                    : null;
            }

            case TransformType.DecodeHtml:
            {
                if (string.IsNullOrWhiteSpace(primaryInput)) return primaryInput;

                return WebUtility.HtmlDecode(primaryInput);
            }

            case TransformType.SrcSetToUrl:
            {
                return ExtractBestImageFromSrcSet(primaryInput);
            }

            default:
            {
                // Safe fallback
                return primaryInput;
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
