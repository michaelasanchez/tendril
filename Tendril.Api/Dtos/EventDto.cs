namespace Tendril.Api.Dtos;

public record EventDto
{
    public Guid Id { get; set; }

    public string? VenueName { get; set; }
    public string? VenueUrl { get; set; }

    public string? Title { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }

    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset? EndUtc { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public string? DetailsUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? TicketUrl { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? UpdatedUtc { get; set; }
}