using Microsoft.Playwright;
using Tendril.Core.Domain.Entities;
using Tendril.Engine.Models;
using Tendril.Engine.Playwright;

namespace Tendril.Engine.Runtime;

public class ScrapeResourceManager(IHttpClientFactory httpClientFactory)
{
    // --- CLIENT MANAGEMENT ---
    // Ensure a client exists. We don't need a disposable return here 
    // because HttpClients from the factory are generally long-lived/managed by the system.
    public HttpClient ResolveClient(ScrapeContext context)
    {
        context.StaticClient ??= httpClientFactory.CreateClient("ScraperClient");

        return context.StaticClient;
    }

    // --- BROWSER MANAGEMENT ---
    // Returns a disposable "Lease". 
    // If we create the browser, the Lease will dispose it when done.
    // If the browser already existed, the Lease does nothing.
    public async Task<BrowserScope> ResolveBrowserScope(ScrapeContext context, ScraperDefinition def)
    {
        bool isOwner = false;

        // TODO: for now we just dispose and recreate the browser if UseRealChrome changes,
        //  but we could be smarter about this if needed
        if (!def.UseHeadlessBrowser && context.DynamicBrowser != null)
        {
            await context.DynamicBrowser.DisposeAsync();

            context.DynamicBrowser = null;
        }

        if (context.DynamicBrowser == null)
        {
            context.DynamicBrowser = await PlaywrightContextFactory.CreateContextAsync(def);
            isOwner = true;
        }

        // Return our custom wrapper that holds the browser AND the logic to kill it
        return new BrowserScope(context, isOwner);
    }

    // --- HELPER CLASSES ---
    public readonly struct BrowserScope(ScrapeContext context, bool isOwner) : IAsyncDisposable
    {
        // The non-nullable reference you wanted!
        public IBrowserContext Browser => context.DynamicBrowser!;

        public async ValueTask DisposeAsync()
        {
            // Only dispose if we are the ones who created it
            if (isOwner && context.DynamicBrowser != null)
            {
                await context.DynamicBrowser.DisposeAsync();
                context.DynamicBrowser = null;
            }
        }
    }
}