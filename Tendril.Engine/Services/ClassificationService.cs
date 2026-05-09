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
            var valueToCompare = string.Empty;
            bool found = true;

            // Split the path by dots and traverse
            foreach (var part in rule.SourceJsonPath.Split('.'))
            {
                if (root.TryGetProperty(part, out var nextElement))
                {
                    root = nextElement;
                }
                else
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                valueToCompare = root.ValueKind == JsonValueKind.String
                    ? root.GetString()
                    : root.GetRawText();
            }

            if (IsMatch(rule, valueToCompare))
            {
                ApplyAssignments(targetEvent, rule.Assignments);
            }
        }
    }

    private void ApplyAssignments(Event targetEvent, IEnumerable<RuleAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            if (assignment.CategoryId.HasValue)
            {
                targetEvent.CategoryId = assignment.CategoryId.Value;
            }

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
        if (rule.ConditionType is ConditionType.Default) return true;

        if (string.IsNullOrEmpty(text)) return false;

        return rule.ConditionType switch
        {
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