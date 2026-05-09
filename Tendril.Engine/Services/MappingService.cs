using HtmlAgilityPack;
using System.Data;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Abstractions;

namespace Tendril.Engine.Services;

public class MappingService : IMappingService
{
    private static readonly Dictionary<string, System.Reflection.PropertyInfo> _eventProperties = typeof(Event)
        .GetProperties()
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

        var rootNode = JsonNode.Parse(raw.RawDataJson);

        if (rootNode == null) return mappedEvent;

        var fields = rootNode["Fields"];
        if (fields == null)
        {
            return mappedEvent;
        }

        var intermediate = new Dictionary<string, TransformResult>();

        foreach (var rule in scraper.MappingRules.OrderBy(x => x.Order))
        {
            ApplyMappingRule(fields, rule, intermediate, referenceYear);
        }

        rootNode["Intermediate"] = JsonSerializer.SerializeToNode(intermediate);

        raw.RawDataJson = rootNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        foreach (var (targetField, transformResult) in intermediate)
        {
            AssignEventField(mappedEvent, targetField, transformResult);
        }

        return mappedEvent;
    }

    private static void ApplyMappingRule(JsonNode raw, ScraperMappingRule rule, Dictionary<string, TransformResult> intermediate, int? referenceYear)
    {
        TransformResult primary = new(null, Type.String);

        // 1. Check Scratchpad first (precedence)
        if (intermediate.TryGetValue(rule.SourceField, out var scratchValue))
        {
            primary = scratchValue;
        }
        // 2. Fallback to Raw JSON
        else if (TryGetValue(raw, rule.SourceField, out var rawValue))
        {
            primary = GetTransformResult(rawValue);
        }
        // 3. If neither, skip
        else if (rule.TransformType != TransformType.Constant)
        {
            return;
        }

        TransformResult? secondary = null;

        if (!string.IsNullOrWhiteSpace(rule.CombineWithField))
        {
            // Same precedence logic for the secondary field
            if (intermediate.TryGetValue(rule.CombineWithField!, out var combineScratchValue))
            {
                secondary = combineScratchValue;
            }
            else if (TryGetValue(raw, rule.CombineWithField!, out var combineRawValue))
            {
                secondary = GetTransformResult(combineRawValue);
            }
        }

        // Transform
        intermediate[rule.TargetField] = ApplyRuleTransform(
            rule,
            primary,
            secondary,
            referenceYear);
    }

    private static TransformResult ApplyRuleTransform(
        ScraperMappingRule rule,
        TransformResult primary,
        TransformResult? secondary,
        int? referenceYear)
    {
        var primaryInput = GetString(primary.Value);
        var secondaryInput = GetString(secondary?.Value);

        switch (rule.TransformType)
        {
            case TransformType.None:
            {
                return primary;
            }

            case TransformType.Constant:
            {
                return new(rule.ConstantValue, Type.String);
            }

            case TransformType.Trim:
            {
                return new(primaryInput?.Trim(), Type.String);
            }

            case TransformType.ToLower:
            {
                return new(primaryInput?.ToLowerInvariant(), Type.String);
            }

            case TransformType.ToUpper:
            {
                return new(primaryInput?.ToUpperInvariant(), Type.String);
            }

            case TransformType.Split:
            {
                if (string.IsNullOrWhiteSpace(primaryInput) || string.IsNullOrWhiteSpace(rule.SplitDelimiter))
                {
                    return new(primaryInput, Type.String);
                }

                return new(primaryInput
                    .Split(rule.SplitDelimiter, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList(),
                    Type.List);
            }

            case TransformType.Combine:
            {
                if (primary.Type is Type.Date && primary.Value is DateTimeOffset datePart)
                {
                    if (secondary?.Type is Type.Time && secondary.Value is DateTimeOffset timePart)
                    {
                        return new(
                            new DateTimeOffset(
                                datePart.Year, datePart.Month, datePart.Day,
                                timePart.Hour, timePart.Minute, timePart.Second,
                                timePart.Offset),
                            Type.DateTime);
                    }

                    return new(datePart, Type.Date);
                }

                var combined = (primaryInput, secondaryInput) switch
                {
                    (string p, string s) => $"{p} {s}",
                    (string p, null) => p,
                    (null, string s) => s,
                    _ => null
                };

                return new(combined, Type.String);
            }

            case TransformType.RegexExtract:
            {
                if (string.IsNullOrWhiteSpace(primaryInput) || string.IsNullOrWhiteSpace(rule.RegexPattern))
                {
                    return new(primaryInput, Type.String);
                }

                var match = Regex.Match(primaryInput, rule.RegexPattern, RegexOptions.Singleline);

                return match.Success ? new(match.Value, Type.String) : new(null, Type.String);
            }

            case TransformType.RegexReplace:
            {
                if (string.IsNullOrWhiteSpace(primaryInput) ||
                    string.IsNullOrWhiteSpace(rule.RegexPattern) ||
                    rule.RegexReplacement is null)
                {
                    return new(primaryInput, Type.String); // TODO: should inherit type form primaryInput
                }

                return new(Regex.Replace(primaryInput, rule.RegexPattern, rule.RegexReplacement), Type.String);
            }

            case TransformType.ParseDate:
            {
                if (DateTimeOffset.TryParse(primaryInput, out var parsed))
                {
                    bool hasTime = primaryInput.Contains(':');

                    // If it's just a date, force the offset to Zero (UTC Midnight).
                    // If it has time, we trust the parsed offset (or the system local).
                    var offset = hasTime ? parsed.Offset : TimeSpan.Zero;

                    var output = new DateTimeOffset(
                        referenceYear ?? parsed.Year,
                        parsed.Month,
                        parsed.Day,
                        hasTime ? parsed.Hour : 0,
                        hasTime ? parsed.Minute : 0,
                        hasTime ? parsed.Second : 0,
                        offset);

                    return new(output, hasTime ? Type.DateTime : Type.Date);
                }

                return new(null, Type.Date);
            }

            case TransformType.ParseTime:
            {
                if (string.IsNullOrWhiteSpace(primaryInput))
                    return new(null, Type.Time);

                // Use DateTime.TryParse to catch the local wall-clock time
                if (DateTime.TryParse(primaryInput, out var timeOnly))
                {
                    // Use Now instead of UtcNow to capture your local -04:00 offset
                    var localNow = DateTimeOffset.Now;

                    var combined = new DateTimeOffset(
                        localNow.Year, localNow.Month, localNow.Day,
                        timeOnly.Hour, timeOnly.Minute, timeOnly.Second,
                        localNow.Offset); // Use the local offset

                    return new(combined, Type.Time);
                }
                return new(null, Type.Time);
            }

            case TransformType.ParseExact:
            {
                if (primaryInput is null || rule.Format is null)
                    return new(null, Type.Date);

                if (DateTimeOffset.TryParseExact(primaryInput, rule.Format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
                {
                    bool hasTime = rule.Format.Any("Hhmms".Contains);

                    var output = hasTime ? parsed : new DateTimeOffset(parsed.Date, TimeSpan.Zero);

                    return new(output, hasTime ? Type.DateTime : Type.Date);
                }
                return new(null, Type.Date);
            }

            case TransformType.Currency:
            {
                if (string.IsNullOrWhiteSpace(primaryInput))
                    return new(null, Type.Number);

                var cleaned = new string(primaryInput.Where(c =>
                    char.IsDigit(c) || c == '.' || c == '-').ToArray());

                return decimal.TryParse(cleaned, out var money)
                    ? new(money, Type.Number)
                    : new(null, Type.Number);
            }

            case TransformType.DecodeHtml:
            {
                if (string.IsNullOrWhiteSpace(primaryInput)) return primary;

                return new(WebUtility.HtmlDecode(primaryInput), Type.String);
            }

            case TransformType.StripHtml:
            {
                if (string.IsNullOrWhiteSpace(primaryInput)) return primary;

                var doc = new HtmlDocument();
                doc.LoadHtml(primaryInput);

                return new(doc.DocumentNode.InnerText, Type.String);
            }

            case TransformType.SrcSetToUrl:
            {
                return new(ExtractBestImageFromSrcSet(primaryInput), Type.String);
            }

            default:
            {
                return primary;
            }
        }
    }

    private static void AssignEventField(Event evt, string targetField, TransformResult result)
    {
        if (result.Value is null) return;

        if (!_eventProperties.TryGetValue(targetField, out var prop))
            return;

        if (prop.PropertyType == typeof(string))
        {
            prop.SetValue(evt, GetString(result.Value));
            return;
        }

        if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
        {
            try
            {
                var d = Convert.ToDecimal(result.Value);
                prop.SetValue(evt, d);
            }
            catch { }

            return;
        }
        if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
        {
            try
            {
                var i = Convert.ToInt32(result.Value);
                prop.SetValue(evt, i);
            }
            catch { }

            return;
        }
        else if (prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTimeOffset?))
        {
            if (result.Value is DateTimeOffset dto)
            {
                prop.SetValue(evt, dto);

                var precision = result.Type == Type.Date
                    ? DatePrecision.Day
                    : DatePrecision.Minute;

                if (targetField.Equals(nameof(Event.StartUtc), StringComparison.OrdinalIgnoreCase))
                {
                    evt.StartPrecision = precision;
                }
                else if (targetField.Equals(nameof(Event.EndUtc), StringComparison.OrdinalIgnoreCase))
                {
                    evt.EndPrecision = precision;
                }
            }

            return;
        }
        else
        {
            prop.SetValue(evt, result.Value);
        }
    }

    private static bool TryGetValue(JsonNode? root, string fieldName, out JsonNode? element)
    {
        // 1. Ensure the root is actually a JSON Object
        if (root is JsonObject obj)
        {
            // 2. TryGetPropertyValue is the direct equivalent to TryGetProperty
            return obj.TryGetPropertyValue(fieldName, out element);
        }

        element = null;
        return false;
    }

    private static string? GetString(object? item)
    {
        if (item is null) return null;

        if (item is string s) return s;

        if (item is JsonNode node)
        {
            return node.GetValueKind() switch
            {
                JsonValueKind.String => node.GetValue<string>(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => node.ToString(),
                JsonValueKind.Array => string.Join(", ", node.AsArray().Select(n => GetString(n))),
                JsonValueKind.Object => node.ToJsonString(),
                _ => null
            };
        }

        if (item is IEnumerable<TransformResult> list)
        {
            return string.Join(", ", list.Select(x => GetString(x.Value)));
        }

        return item.ToString();
    }

    private static TransformResult GetTransformResult(JsonNode? node)
    {
        if (node == null)
            return new TransformResult(null, Type.String);

        return node.GetValueKind() switch
        {
            JsonValueKind.String => new TransformResult(node.GetValue<string>(), Type.String),
            JsonValueKind.Number => new TransformResult(node.GetValue<decimal>(), Type.Number),
            JsonValueKind.True => new TransformResult(true, Type.Boolean),
            JsonValueKind.False => new TransformResult(false, Type.Boolean),
            JsonValueKind.Array => new TransformResult(
                node.AsArray().Select(GetTransformResult).ToList(),
                Type.List
            ),
            JsonValueKind.Object => new TransformResult(node.ToJsonString(), Type.String),
            JsonValueKind.Null => new TransformResult(null, Type.String),

            _ => new TransformResult(null, Type.String)
        };
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

    private enum Type
    {
        String = 0,
        Number = 1,
        Boolean = 2,
        DateTime = 3,
        Date = 4,
        Time = 5,
        List = 6
    }

    private record TransformResult(object? Value, Type Type);

}
