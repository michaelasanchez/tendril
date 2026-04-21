namespace Tendril.Api.Dtos;

public enum ReorderDirection
{
    Up,
    Down
}

public class ReorderActionRequest
{
    public ReorderDirection Direction { get; set; }
}
