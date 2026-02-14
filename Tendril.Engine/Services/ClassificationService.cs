using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Data;
using Tendril.Engine.Interfaces;

namespace Tendril.Engine.Services;

public class ClassificationService(TendrilDbContext context) : IClassificationService
{
    public void ClassifyEvent(ScraperDefinition scraper, ScrapedEventRaw mappedEvent, Event targetEvent)
    {// 1. Parse ONCE outside the loop for performance
        using var jsonDoc = JsonDocument.Parse(mappedEvent.RawDataJson);
        var root = jsonDoc.RootElement;

        var rules = context.ClassificationRules
            .Include(r => r.Assignments)
            .Where(r => r.ScraperDefinitionId == scraper.Id && !r.Disabled)
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var rule in rules)
        {
            // 2. Use TryGetProperty on the root element
            if (root.TryGetProperty(rule.SourceJsonPath, out var jsonElement))
            {
                // jsonElement.GetString() works if the value is a string. 
                // GetRawText() is a safer fallback if it's a number/bool you want to match as text.
                string? valueToCompare = jsonElement.ValueKind == JsonValueKind.String
                    ? jsonElement.GetString()
                    : jsonElement.GetRawText();

                if (IsMatch(rule, valueToCompare))
                {
                    ApplyAssignments(targetEvent, rule.Assignments);
                }
            }
        }
    }

    private void ApplyAssignments(Event targetEvent, IEnumerable<RuleAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            // 2. Handle Category Assignment (1-to-1)
            if (assignment.CategoryId.HasValue)
            {
                targetEvent.CategoryId = assignment.CategoryId.Value;
            }

            // 3. Handle Tag Assignment (Many-to-Many)
            if (assignment.TagId.HasValue)
            {
                // Avoid duplicates if the tag is already there
                if (!targetEvent.EventTags.Any(et => et.TagId == assignment.TagId.Value))
                {
                    targetEvent.EventTags.Add(new EventTag
                    {
                        EventId = targetEvent.Id,
                        TagId = assignment.TagId.Value
                    });
                }
            }
        }
    }

    private static bool IsMatch(ScraperClassificationRule rule, string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        return rule.ConditionType switch
        {
            ConditionType.Default => true,
            ConditionType.Equals => text.Equals(rule.ConditionValue, StringComparison.OrdinalIgnoreCase),
            ConditionType.NotEquals => !text.Equals(rule.ConditionValue, StringComparison.OrdinalIgnoreCase),
            ConditionType.Contains => text.Contains(rule.ConditionValue, StringComparison.OrdinalIgnoreCase),
            ConditionType.NotContains => !text.Contains(rule.ConditionValue, StringComparison.OrdinalIgnoreCase),
            ConditionType.StartsWith => text.StartsWith(rule.ConditionValue, StringComparison.OrdinalIgnoreCase),
            ConditionType.EndsWith => text.EndsWith(rule.ConditionValue, StringComparison.OrdinalIgnoreCase),
            ConditionType.GreaterThan => string.Compare(text, rule.ConditionValue, StringComparison.OrdinalIgnoreCase) > 0,
            ConditionType.LessThan => string.Compare(text, rule.ConditionValue, StringComparison.OrdinalIgnoreCase) < 0,
            ConditionType.GreaterThanOrEqualTo => string.Compare(text, rule.ConditionValue, StringComparison.OrdinalIgnoreCase) >= 0,
            ConditionType.LessThanOrEqualTo => string.Compare(text, rule.ConditionValue, StringComparison.OrdinalIgnoreCase) <= 0,
            ConditionType.RegexMatch => System.Text.RegularExpressions.Regex.IsMatch(text, rule.ConditionValue),
            ConditionType.RegexNotMatch => !System.Text.RegularExpressions.Regex.IsMatch(text, rule.ConditionValue),
            _ => false // Add other cases as needed
        };
    }
}