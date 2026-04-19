using Microsoft.Playwright;
using Tendril.Core.Domain.Entities;
using Tendril.Engine.Models;
using Tendril.Engine.Playwright;

namespace Tendril.Engine.Runtime;

public class ScrapeResourceManager(
    IHttpClientFactory httpClientFactory,
    PlaywrightContextFactory playwrightFactory)
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

        // TODO: for now we just dispose and recreate the browser if this option changes,
        //  but we could be smarter about this if needed -> Dictionary<int, IBrowserContext>
        if (!def.UseHeadlessBrowser && context.BrowserContext != null)
        {
            await context.BrowserContext.DisposeAsync();

            context.BrowserContext = null;
        }

        if (context.BrowserContext == null)
        {
            context.BrowserContext = await playwrightFactory.CreateContextAsync(def);
            isOwner = true;
        }

        // Return our custom wrapper that holds the browser AND the logic to kill it
        return new BrowserScope(context, isOwner);
    }

    // --- HELPER CLASSES ---
    public readonly struct BrowserScope(ScrapeContext context, bool isOwner) : IAsyncDisposable
    {
        public IBrowserContext BrowserContext => context.BrowserContext!;

        public IPage? GetPage() => BrowserContext!.Pages.FirstOrDefault();

        public async ValueTask DisposeAsync()
        {
            if (isOwner && context.BrowserContext != null)
            {
                try
                {
                    await context.BrowserContext.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
                }
                catch (TimeoutException)
                {
                    // stale connection, context is effectively dead anyway
                }
                finally
                {
                    context.BrowserContext = null;
                }
            }
        }
    }
}