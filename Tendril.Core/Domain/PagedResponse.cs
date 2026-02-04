namespace Tendril.Core.Domain;

public class PagedResponse<T>
{
    public List<T> Items { get; set; }
    public Guid? NextCursor { get; set; }
    public bool HasNextPage { get; set; }
}
