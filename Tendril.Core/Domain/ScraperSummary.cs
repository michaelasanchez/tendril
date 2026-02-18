namespace Tendril.Core.Domain;

public static class TargetField
{
    public const string Title = "Title";
    public const string Description = "Description";
    public const string Location = "Location";
    public const string Venue = "Venue";
    public const string StartUtc = "StartUtc";
    public const string EndUtc = "EndUtc";
    public const string MinPrice = "MinPrice";
    public const string MaxPrice = "MaxPrice";
    public const string DetailsUrl = "DetailsUrl";
    public const string ImageUrl = "ImageUrl";
    public const string TicketUrl = "TicketUrl";
}

public class MappingSummary
{
    public bool Title { get; set; }
    public bool Description { get; set; }

    public bool Location { get; set; }
    public bool Venue { get; set; }

    public bool StartUtc { get; set; }
    public bool EndUtc { get; set; }

    public bool MinPrice { get; set; }
    public bool MaxPrice { get; set; }

    public bool DetailsUrl { get; set; }
    public bool ImageUrl { get; set; }
    public bool TicketUrl { get; set; }

}

public class ScraperSummary
{
    public MappingSummary Mapping { get; set; } = new MappingSummary();

    //public Guid Id { get; set; }
    //public string Name { get; set; } = null!;
    //public int TotalAttempts { get; set; }
    //public int SuccessfulAttempts { get; set; }
    //public DateTimeOffset? LastAttemptTimeUtc { get; set; }
}
