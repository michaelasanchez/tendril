namespace Tendril.Core.Domain.Entities;

public class RuleAssignment
{
    public Guid Id { get; set; }
    public Guid ScraperClassificationRuleId { get; set; }
    public ScraperClassificationRule ScraperClassificationRule { get; set; } = null!;
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid? TagId { get; set; }
    public Tag? Tag { get; set; }
}
