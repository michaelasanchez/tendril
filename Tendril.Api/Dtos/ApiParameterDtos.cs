using Tendril.Core.Domain.Entities;

namespace Tendril.Api.Dtos;

public record ApiParameterDto(
    Guid Id,
    Guid ScraperDefinitionId,
    string Key,
    string Template,
    ApiParameterSource Source,
    ApiParameterTarget Target,
    bool IsRequired
);

public class CreateApiParameterRequest
{
    public string Key { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public ApiParameterSource Source { get; set; } = ApiParameterSource.Parent;
    public ApiParameterTarget Target { get; set; } = ApiParameterTarget.Query;
    public bool IsRequired { get; set; } = false;
}

public class UpdateApiParameterRequest
{
    public Guid? Id { get; set; }
    public string? Key { get; set; }
    public string? Template { get; set; }
    public ApiParameterSource? Source { get; set; }
    public ApiParameterTarget? Target { get; set; }
    public bool? IsRequired { get; set; }
}
