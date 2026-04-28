using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using System.Net.Http.Json;

namespace KnightsAndGM.UiTests;

public sealed class CampaignBrowserTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public CampaignBrowserTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task PlayerCanOpenMapMoveOneHexAndPostANote()
    {
        await using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            username = "browser-player",
            role = "Player"
        });
        response.EnsureSuccessStatusCode();

        var page = await browser.NewPageAsync(new BrowserNewPageOptions { BaseURL = "http://localhost" });
        await page.GotoAsync("/");
        await page.FillAsync("#login-username", "browser-player");
        await page.ClickAsync("#login-form button[type=submit]");
        await page.WaitForSelectorAsync(".hex-tile");

        var moveButtons = await page.Locator("button:has-text('Move Party')").AllAsync();
        if (moveButtons.Count > 0)
        {
            await moveButtons[0].ClickAsync();
        }

        var travelButtons = await page.Locator("button:has-text('Travel Here')").AllAsync();
        if (travelButtons.Count > 0)
        {
            await travelButtons[0].ClickAsync();
        }

        await page.FillAsync("#note-body", "Leaving a test trail for the next expedition.");
        await page.ClickAsync("#note-form button[type=submit]");
    }
}
