namespace Tendril.Api.Dtos;

public record TestRunResultDto(
    bool Success,
    string? ErrorMessage,
    List<Dictionary<string, string?>> RawEvents
);