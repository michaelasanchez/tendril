using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record PatchEventRequest
{
    public string? CategoryId { get; set; }
    public EventStatus? Status { get; set; } = null;
};