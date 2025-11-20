using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Engine.Playwright;

using Microsoft.Playwright;

public static class PlaywrightContextFactory
{
    private static IPlaywright? _pw;

    public static async Task<IPage> CreatePageAsync()
    {
        _pw ??= await Playwright.CreateAsync();
        var browser = await _pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync();
        return await context.NewPageAsync();
    }
}
