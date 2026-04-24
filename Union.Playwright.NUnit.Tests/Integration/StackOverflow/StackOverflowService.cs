using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.Tests.Integration.StackOverflow;

public class StackOverflowService : UnionService<StackOverflowPage>
{
    public StackOverflowService(TestSettings? testSettings = null)
        : base(testSettings)
    {
    }

    public override string BaseUrl => "https://stackoverflow.com";

    public async Task MockQuestionsAsync(string htmlContent)
    {
        var page = await this.GetOrCreatePageAsync();
        await page.RouteAsync("**/questions", async route =>
        {
            await route.FulfillAsync(new()
            {
                ContentType = "text/html",
                Body = htmlContent
            });
        });
    }

    public async Task UnmockQuestionsAsync()
    {
        var page = await this.GetOrCreatePageAsync();
        await page.UnrouteAsync("**/questions");
    }
}
