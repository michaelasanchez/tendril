namespace Tendril.Core.Domain.Entities;

public class EventTag
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
