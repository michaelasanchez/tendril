namespace Tendril.Api.Dtos;

public record EventDto
{
    public Guid Id { get; set; }

    public string? VenueName { get; set; }
    public string? VenueUrl { get; set; }

    public string? Title { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public DateTimeOffset? StartDateTime { get; set; }
    public DateOnly? StartDate { get; set; }
    public bool ShowStartTime { get; set; }
    public DateTimeOffset? EndDateTime { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool? ShowEndTime { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public string? DetailsUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? TicketUrl { get; set; }

    public string Status { get; set; } = string.Empty;
    public bool RequiresReview { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? ReviewRequiredAtUtc { get; set; }
}