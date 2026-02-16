namespace Tendril.Api.Dtos;

public record RuleAssignmentDto(
    Guid Id,
    Guid? CategoryId,
    Guid? TagId
);

public class CreateRuleAssignment
{
    public Guid? CategoryId { get; set; }
    public Guid? TagId { get; set; }
}

public class UpdateRuleAssignment
{
    public Guid? Id { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? TagId { get; set; }
}