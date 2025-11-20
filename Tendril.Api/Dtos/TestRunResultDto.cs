namespace Tendril.Api.Dtos;

public record TestRunResultDto(
    bool Success,
    string? ErrorMessage,
    List<Dictionary<string, List<string>?>> RawEvents
);