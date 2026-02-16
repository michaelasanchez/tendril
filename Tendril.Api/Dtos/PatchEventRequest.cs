using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record PatchEventRequest
{
    public Guid? CategoryId { get; set; }
    public EventStatus? Status { get; set; } = null;
};