namespace Tendril.Core.Domain.Entities;

public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<EventTag> EventTags { get; set; } = [];
}
