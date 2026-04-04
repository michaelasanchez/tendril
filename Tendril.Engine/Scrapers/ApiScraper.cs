using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Interfaces;
using Tendril.Engine.Models;

namespace Tendril.Engine.Scrapers;

public class ApiScraper(ITemplateService templateService)
{
    public async IAsyncEnumerable<ScrapeYieldItem> ExecuteAsync(
        HttpClient client,
        ScraperDefinition def,
        ScrapeContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // This allows the template to use {{ Parent.VenueName }}
        var templateContext = new Dictionary<string, object>
        {
            ["Parent"] = context.ParentData?.Fields ?? []
        };

        // 2. Prepare Request Components
        var queryParams = System.Web.HttpUtility.ParseQueryString(string.Empty);
        var headers = new Dictionary<string, string>();

        // 3. TRANSLATION: Apply the Rules from ApiParameters
        foreach (var rule in def.Parameters)
        {
            // Resolve the value (e.g., "XXX Presents YYY" -> "YYY")
            var resolvedValue = templateService.Render(rule.Template, templateContext);

            switch (rule.Target)
            {
                case ApiParameterTarget.Query:
                    queryParams[rule.Key] = resolvedValue;
                    break;
                case ApiParameterTarget.Header:
                    headers[rule.Key] = resolvedValue;
                    break;
            }
        }

        // 4. Build the Final URL
        var uriBuilder = new UriBuilder(def.BaseUrl);
        uriBuilder.Query = queryParams.ToString();

        var method = def.Method == Core.Domain.Enums.HttpMethod.GET
            ? System.Net.Http.HttpMethod.Get
            : System.Net.Http.HttpMethod.Post;

        // 5. Construct the Request
        var request = new HttpRequestMessage(method, uriBuilder.Uri.ToString());
        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // 6. FETCH & YIELD (Using your Container Pattern)
        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);

        // Using JsonNode for easier path-based navigation
        var root = JsonNode.Parse(json);

        // 3. CONTAINER PATTERN
        var containerSelector = def.Selectors.SingleOrDefault(x => x.Type == SelectorType.Container);
        if (containerSelector == null) yield break;

        // Extract items using the Container's "Path" (JsonPath)
        // Note: You may need a library like 'JsonPath.Net' or a custom helper here
        var items = ExtractJsonNodes(root, containerSelector.Selector);

        foreach (var node in items)
        {
            var result = new ScrapeYieldItem();
            var partial = false;

            // 4. FIELD EXTRACTION
            var fieldSelectors = def.Selectors
                .Where(x => x.Type != SelectorType.Container && !x.IsPaginationTrigger)
                .OrderBy(x => x.Order);

            foreach (var step in fieldSelectors)
            {
                if (step.ChildScraperDefinitionId.HasValue)
                {
                    // Logic to extract a URL/ID for the next scraper
                    var childUrl = node?[step.Selector]?.ToString();
                    if (!string.IsNullOrEmpty(childUrl))
                    {
                        result = result with
                        {
                            ChildUrl = childUrl,
                            ChildScraperId = step.ChildScraperDefinitionId
                        };
                    }
                }
                else
                {
                    // Standard field extraction from the current JSON node
                    var val = node?[step.Selector]?.ToString();
                    if (val != null) result.Data.Fields[step.FieldName] = val;
                }
            }

            yield return result;
        }
    }

    private IEnumerable<JsonNode?> ExtractJsonNodes(JsonNode? root, string path)
    {
        if (root == null || string.IsNullOrWhiteSpace(path)) return Enumerable.Empty<JsonNode?>();

        // Split the path (e.g., "_embedded.events") and navigate down
        JsonNode? current = root;
        var segments = path.Split('.');

        foreach (var segment in segments)
        {
            current = current?[segment];
        }

        return current?.AsArray() ?? Enumerable.Empty<JsonNode?>();
    }
}