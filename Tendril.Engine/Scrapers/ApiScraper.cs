using Json.Path;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Extensions;
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
        var templateContext = new Dictionary<string, object>
        {
            ["Parent"] = context.ParentItem?.Data.Fields ?? []
        };

        var queryParams = System.Web.HttpUtility.ParseQueryString(string.Empty);
        var headers = new Dictionary<string, string>();

        // Apply the Rules from ApiParameters
        foreach (var rule in def.Parameters)
        {
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

        // Construct the Request
        var uriBuilder = new UriBuilder(def.BaseUrl)
        {
            Query = queryParams.ToString()
        };

        var method = def.Method == Core.Domain.Enums.HttpMethod.GET
            ? System.Net.Http.HttpMethod.Get
            : System.Net.Http.HttpMethod.Post;

        var request = new HttpRequestMessage(method, uriBuilder.Uri.ToString());
        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Check duplicate url
        var uri = request.RequestUri?.ToString();
        // step.AllowDuplicateUrls is true || !context.HasVisited(url)
        //  (A || !B)
        // !(A || !B)
        // !A && B
        if (uri is null || (context.ParentItem?.AllowDuplicateUrls is false && context.HasVisited(uri)))
        {
            yield break;
        }

        context.MarkVisited(uri);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var root = JsonNode.Parse(json);

        var preResult = (context.ParentItem ?? new ScrapeYieldItem()) with
        {
            ChildScraperId = null,
            ChildUrl = null,
        };

        // TODO: Need to perform pre container selectors here

        var containerSelector = def.Actions.SingleOrDefault(x => x.Type == ActionType.Container);
        if (containerSelector == null) yield break;

        var items = ExtractJsonNodes(root, containerSelector.Selector);

        var foundChildren = false;

        foreach (var node in items)
        {
            var result = new ScrapeYieldItem();

            var fieldSelectors = def.Actions
                .Where(x => x.Type != ActionType.Container && !x.IsPaginationTrigger)
                .OrderBy(x => x.Order);


            foreach (var step in fieldSelectors)
            {
                // We evaluate the field selector RELATIVE to the current 'node'
                var fieldMatch = ExtractJsonNodes(node, step.Selector).FirstOrDefault();
                var val = fieldMatch?.ToString();

                if (step.Type == ActionType.FollowLink && step.ChildScraperDefinitionId.HasValue)
                {
                    if (!string.IsNullOrEmpty(val))
                    {
                        result = result with
                        {
                            ChildScraperId = step.ChildScraperDefinitionId,
                            ChildUrl = val,
                            AllowEmptyResult = step.AllowDuplicateUrls
                        };
                    }
                }
                else if (step.Type == ActionType.CallApi && step.ChildScraperDefinitionId.HasValue)
                {
                    result = result with
                    {
                        ChildScraperId = step.ChildScraperDefinitionId,
                        AllowDuplicateUrls = step.AllowDuplicateUrls,
                        AllowEmptyResult = step.AllowEmptyResult
                    };
                }
                else if (val != null)
                {
                    result.Data.Fields[step.OutputField] = val;
                }
            }

            foundChildren = true;

            yield return preResult.Merge(result);
        }

        if (!items.Any() && context.ParentItem?.AllowEmptyResult is true)
        {
            yield return preResult;
        }
    }

    private static IEnumerable<JsonNode?> ExtractJsonNodes(JsonNode? root, string path)
    {
        if (root == null || string.IsNullOrWhiteSpace(path))
            return [];

        // Parse the JsonPath
        if (!JsonPath.TryParse(path, out var jsonPath))
        {
            // Handle invalid path syntax here
            return [];
        }

        // Evaluate against the root
        var evaluation = jsonPath.Evaluate(root);

        // Evaluation.Matches contains the nodes found at that path
        return evaluation.Matches.Select(m => m.Value);
    }
}