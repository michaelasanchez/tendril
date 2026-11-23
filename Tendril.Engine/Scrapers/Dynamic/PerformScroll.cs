using Microsoft.Playwright;

namespace Tendril.Engine.Scrapers.Dynamic;

public partial class DynamicScraper
{
    private static async Task<bool> PerformScroll(
    IPage page,
    IElementHandle? container = null,
    int growthTimeoutMs = 8000,
    int settleMs = 600
    )
    {
        const string heightJs = "el => (el ?? document.scrollingElement).scrollHeight";
        // Scrolls one step and reports whether we're at the (live) bottom
        const string stepJs = @"el => {
        const e = el ?? document.scrollingElement;
        e.scrollBy({ top: e.clientHeight * 0.9 });
        return e.scrollTop + e.clientHeight >= e.scrollHeight - 2;
    }";
        const string nudgeJs = @"el => {
        const e = el ?? document.scrollingElement;
        e.scrollBy({ top: -400 });
        setTimeout(() => e.scrollTo({ top: e.scrollHeight }), 150);
    }";

        Task<double> Height() => page.EvaluateAsync<double>(heightJs, container);

        var vp = page.ViewportSize;
        await page.Mouse.MoveAsync((vp?.Width ?? 1280) / 2, (vp?.Height ?? 720) / 2);

        double start = await Height();

        // 1. Walk to the current bottom so everything already on the page renders
        for (int i = 0; i < 100; i++)
        {
            await page.Mouse.WheelAsync(0, 600);
            bool atBottom = await page.EvaluateAsync<bool>(stepJs, container);
            await Task.Delay(100);
            if (atBottom) break;
        }

        // 2. Wait for the page to grow, nudging the bottom if nothing happens
        var deadline = DateTime.UtcNow.AddMilliseconds(growthTimeoutMs);
        double height = await Height();
        int polls = 0;

        while (height <= start && DateTime.UtcNow < deadline)
        {
            await Task.Delay(200);
            height = await Height();

            if (height <= start && ++polls % 8 == 0)   // roughly every 1.6s
                await page.EvaluateAsync(nudgeJs, container);
        }

        if (height <= start)
            return false;

        // 3. Grew: wait until it stops changing so the caller sees a stable page
        var lastChange = DateTime.UtcNow;
        while ((DateTime.UtcNow - lastChange).TotalMilliseconds < settleMs)
        {
            await Task.Delay(100);
            double h = await Height();
            if (h != height)
            {
                height = h;
                lastChange = DateTime.UtcNow;
            }
        }

        return true;
    }
}
