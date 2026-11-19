using Tendril.Core.Domain.Entities;

namespace Tendril.Api.Dtos;

public record IngestResultDto
{
    public AttemptHistoryDto? Attempt { get; set; }
    public AttemptStatus Status { get; set; }
    public List<string>? Errors { get; set; }

    public List<ScrapedEventRawDto>? Raw { get; set; }
    public List<EventDto>? Mapped { get; set; }
    public List<string>? MappingSummary { get; set; }
}
