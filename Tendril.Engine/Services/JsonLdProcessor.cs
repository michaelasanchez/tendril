using System.Text.Json;
using System.Text.RegularExpressions;
using Tendril.Engine.Interfaces;
using Tendril.Engine.Models;

namespace Tendril.Engine.Services;

public class JsonLdProcessor : IJsonLdProcessor
{
    public IEnumerable<RawScrapedData> ExtractAll(string htmlContent, string targetType)
    {
        var matches = Regex.Matches(
            htmlContent,
            @"(?is)<script[^>]*?type\s*=\s*['""]application/ld\+json['""][^>]*?>(.*?)</script>",
            RegexOptions.Compiled);

        foreach (Match match in matches)
        {
            var json = match.Groups[1].Value;
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch { continue; }

            using (doc)
            {
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in root.EnumerateArray())
                    {
                        if (IsMatch(element, targetType))
                            yield return MapJson(element);
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (IsMatch(root, targetType))
                        yield return MapJson(root);
                }
            }
        }
    }

    private bool IsMatch(JsonElement element, string targetType)
    {
        if (element.TryGetProperty("@type", out var typeProp))
        {
            return typeProp.ToString().Contains(targetType, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private RawScrapedData MapJson(JsonElement element)
    {
        var evt = new RawScrapedData();
        Flatten(element, string.Empty, evt.Fields);
        return evt;
    }

    private void Flatten(JsonElement element, string prefix, Dictionary<string, string> fields)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                    Flatten(prop.Value, key, fields);
                }
                break;

            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    Flatten(item, $"{prefix}[{index}]", fields);
                    index++;
                }
                break;

            case JsonValueKind.String:
                fields[prefix] = element.GetString() ?? string.Empty;
                break;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                break;

            default:
                fields[prefix] = element.ToString();
                break;
        }
    }
}