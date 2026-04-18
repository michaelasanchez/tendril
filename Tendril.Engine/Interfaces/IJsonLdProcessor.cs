using Tendril.Engine.Models;

namespace Tendril.Engine.Interfaces;

public interface IJsonLdProcessor
{
    IEnumerable<RawScrapedData> ExtractAll(string htmlContent, string targetType);
}