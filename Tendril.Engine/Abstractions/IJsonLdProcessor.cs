using Tendril.Engine.Models;

namespace Tendril.Engine.Abstractions;

public interface IJsonLdProcessor
{
    RawScrapedEvent? Extract(string htmlContent, string targetType);
}