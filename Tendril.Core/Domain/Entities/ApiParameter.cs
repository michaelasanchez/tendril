namespace Tendril.Core.Domain.Entities;

public class ApiParameter
{
    public Guid Id { get; set; }
    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition ScraperDefinition { get; set; } = null!;

    public string Key { get; set; } = null!;

    public string Template { get; set; } = null!;

    public ApiParameterSource Source { get; set; }

    public ApiParameterTarget Target { get; set; } = ApiParameterTarget.Query;

    public bool IsRequired { get; set; } = true;


    //public Guid Id { get; set; }
    //public string Key { get; set; } = null!; // e.g., "Authorization" or "venueId"
    //public string ValueTemplate { get; set; } = null!;

    //public ApiParameterSource Source { get; set; } = ApiParameterSource.Static;
    //public ApiParameterTarget Target { get; set; } = ApiParameterTarget.Query; // Where does it go?

    //public Guid ApiConfigurationId { get; set; }
}

public enum ApiParameterSource
{
    Static,       // A fixed value
    Parent, // Pulled from the parent scraper's extracted data
    //Invocation
}

public enum ApiParameterTarget
{
    Query,   // Appends to URL: ?key=value
    Header,  // Adds to HttpRequestHeaders
    Body     // Injected into a JSON body (if you're doing complex POSTs)
}