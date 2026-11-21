using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public class ScraperAction
{
    public Guid Id { get; set; }

    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition ScraperDefinition { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string FieldName { get; set; } = null!;
    public string OutputField => string.IsNullOrEmpty(FieldName) ? Name : FieldName;

    public int Order { get; set; } = 0;
    public bool Root { get; set; } = false;
    public ActionType Type { get; set; } = ActionType.Text;

    public string Selector { get; set; } = null!;
    public string? AttributeName { get; set; }
    public SynchronizationCondition? Synchronization { get; set; }
    public int? Delay { get; set; }

    public string? ConstantValue { get; set; }
    public string? InteractionValue { get; set; }

    public Guid? ChildScraperDefinitionId { get; set; }
    public bool? AllowDuplicateUrls { get; set; }
    public bool? AllowEmptyResult { get; set; }

    public bool IsPaginationTrigger { get; set; }

    public bool Disabled { get; set; } = false;
}
