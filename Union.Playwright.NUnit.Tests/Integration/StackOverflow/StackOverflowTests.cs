using FluentAssertions;
using NUnit.Framework;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages;
using Union.Playwright.NUnit.TestSession;

namespace Union.Playwright.NUnit.Tests.Integration.StackOverflow;

[TestFixture]
public class StackOverflowTests : UnionTest<StackOverflowTestSession>
{
    protected override TestSessionProvider<StackOverflowTestSession> GetSessionProvider()
        => StackOverflowTestSessionProvider.Instance;

    [SetUp]
    public async Task SetUpRoute()
    {
        // Load the HTML fixture file (copied to output directory)
        var htmlPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Integration", "StackOverflow", "questions.html");
        var htmlContent = await File.ReadAllTextAsync(htmlPath);

        // Get the page from the service
        var page = await Session.SO.GetOrCreatePageAsync();

        // Intercept requests to stackoverflow.com and serve local HTML
        await page.RouteAsync("**/questions", async route =>
        {
            await route.FulfillAsync(new()
            {
                ContentType = "text/html",
                Body = htmlContent
            });
        });
    }

    [Test]
    public async Task NavigateToQuestions_PageIsQuestionsPage()
    {
        var page = await Session.SO.Go.ToPage<QuestionsPage>();

        Session.SO.State.PageIs<QuestionsPage>().Should().BeTrue();
        page.Should().NotBeNull();
    }

    [Test]
    public async Task QuestionsPage_GetIds_ReturnsNonEmpty()
    {
        var page = await Session.SO.Go.ToPage<QuestionsPage>();
        page.Should().NotBeNull("ToPage should return a QuestionsPage");
        page!.Questions.Should().NotBeNull("Questions should be initialized by WebPageBuilder");

        var ids = await page.Questions.GetIdsAsync();

        ids.Should().NotBeEmpty();
        ids.Should().HaveCount(3);
        ids.Should().Contain("79880001");
        ids.Should().Contain("79880002");
        ids.Should().Contain("79880003");
    }

    [Test]
    public async Task QuestionsPage_GetItems_TitleIsInitializedAndVisible()
    {
        var page = await Session.SO.Go.ToPage<QuestionsPage>();
        var items = await page!.Questions.GetItemsAsync();
        items.Should().HaveCount(3);

        var firstItem = items[0];

        firstItem.Title.Should().NotBeNull();
        var isVisible = await firstItem.Title.IsVisibleAsync();
        isVisible.Should().BeTrue();
    }

    [Test]
    public async Task QuestionsPage_FirstItem_HasTags()
    {
        var page = await Session.SO.Go.ToPage<QuestionsPage>();
        var firstItem = await page!.Questions.FindSingleAsync();
        firstItem.Should().NotBeNull();

        var tagNames = await firstItem!.Tags.GetTagNamesAsync();
        tagNames.Should().NotBeEmpty();
        tagNames.Should().Contain("playwright");
        tagNames.Should().Contain("testing");
        tagNames.Should().Contain("csharp");
    }

    [Test]
    public async Task QuestionsPage_FirstItem_TagLinkIsVisible()
    {
        var page = await Session.SO.Go.ToPage<QuestionsPage>();
        var firstItem = await page!.Questions.FindSingleAsync();
        firstItem.Should().NotBeNull();

        firstItem!.Tags.Should().NotBeNull();
        firstItem.Tags.TagLink.Should().NotBeNull();

        var isVisible = await firstItem.Tags.TagLink.First.IsVisibleAsync();
        isVisible.Should().BeTrue();
    }
}
