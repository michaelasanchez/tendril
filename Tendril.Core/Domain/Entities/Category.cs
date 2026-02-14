namespace Tendril.Core.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public List<Event> Events { get; set; } = new();
}
