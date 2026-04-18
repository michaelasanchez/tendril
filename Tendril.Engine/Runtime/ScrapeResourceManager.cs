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

        if (context.DynamicBrowser == null || def.UseRealChrome)
        {
            context.DynamicBrowser = await PlaywrightContextFactory.CreateContextAsync(def.UseRealChrome);
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

    private class NoOpDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private class BrowserOwnerDisposable(ScrapeContext context) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            if (context.DynamicBrowser != null)
            {
                await context.DynamicBrowser.DisposeAsync();
                context.DynamicBrowser = null; // Null it out so upstream logic knows it's gone
            }
        }
    }
}