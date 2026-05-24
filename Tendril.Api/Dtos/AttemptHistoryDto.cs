namespace Tendril.Api.Dtos;

public class AttemptHistoryDto
{
    public Guid Id { get; init; }
    public string? ScraperName { get; init; }

    public DateTimeOffset StartTimeUtc { get; init; }
    public DateTimeOffset? EndTimeUtc { get; init; }

    public bool Success { get; init; } = default!;
    public int Extracted { get; init; }
    public int Mapped { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Errored { get; set; }
    public string? ErrorMessage { get; init; }
}
