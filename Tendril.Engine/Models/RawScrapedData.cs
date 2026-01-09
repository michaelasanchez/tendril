using System.Text;

namespace Tendril.Engine.Models;

public class RawScrapedData
{
    public Dictionary<string, string?> Fields { get; set; } = [];

    public string GetSignature()
    {
        if (Fields.Count == 0) return string.Empty;

        // 1. Sort keys alphabetically so order doesn't matter
        var sortedKeys = Fields.Keys.OrderBy(k => k);

        var sb = new StringBuilder();

        foreach (var key in sortedKeys)
        {
            var value = Fields[key] ?? "null";

            // Create a format like: [Title:MyEvent][Date:2024-01-01]
            sb.Append($"[{key}:{value}]");
        }

        // Returns a unique string representation of the content
        return sb.ToString();
    }
}