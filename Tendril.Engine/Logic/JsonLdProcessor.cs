using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Models;

namespace Tendril.Engine.Logic;

public class JsonLdProcessor : IJsonLdProcessor
{
    public RawScrapedEvent? Extract(string htmlContent, string targetType)
    {
        var matches = Regex.Matches(
            htmlContent,
            @"(?is)<script[^>]*?type\s*=\s*['""]application/ld\+json['""][^>]*?>(.*?)</script>",
            RegexOptions.Compiled);

        foreach (Match match in matches)
        {
            var json = match.Groups[1].Value;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Handle Array of JSON-LD objects
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in root.EnumerateArray())
                    {
                        if (IsMatch(element, targetType)) return MapJson(element);
                    }
                }
                // Handle Single Object
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (IsMatch(root, targetType)) return MapJson(root);
                }
            }
            catch { /* Invalid JSON in page, ignore */ }
        }

        return null;
    }

    private bool IsMatch(JsonElement element, string targetType)
    {
        if (element.TryGetProperty("@type", out var typeProp))
        {
            var typeVal = typeProp.ToString();
            return typeVal.Contains(targetType, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private RawScrapedEvent MapJson(JsonElement element)
    {
        var evt = new RawScrapedEvent();
        // Start the recursive flattening
        Flatten(element, string.Empty, evt.Fields);
        return evt;
    }

    /// <summary>
    /// Recursively traverses the JsonElement.
    /// - If it's a primitive, it adds it to the dictionary.
    /// - If it's an Object, it appends ".PropertyName" to the key and recurses.
    /// - If it's an Array, it appends "[Index]" to the key and recurses.
    /// </summary>
    private void Flatten(JsonElement element, string prefix, Dictionary<string, string> fields)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    // Build the key: "location.address"
                    string key = string.IsNullOrEmpty(prefix)
                        ? prop.Name
                        : $"{prefix}.{prop.Name}";

                    Flatten(prop.Value, key, fields);
                }
                break;

            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    // Build the key: "performers[0]"
                    string key = $"{prefix}[{index}]";
                    Flatten(item, key, fields);
                    index++;
                }
                break;

            case JsonValueKind.String:
                var rawValue = element.GetString() ?? string.Empty;

                // This converts "&lsquo;" to "‘" and "&amp;" to "&"
                fields[prefix] = WebUtility.HtmlDecode(rawValue);
                break;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                // Skip nulls or set them to empty string depending on preference
                break;

            default:
                // Handles Number, True, False
                fields[prefix] = element.ToString();
                break;
        }
    }
}