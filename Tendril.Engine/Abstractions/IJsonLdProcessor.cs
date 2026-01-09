using Tendril.Engine.Models;

namespace Tendril.Engine.Abstractions;

public interface IJsonLdProcessor
{
    RawScrapedData? Extract(string htmlContent, string targetType);
}