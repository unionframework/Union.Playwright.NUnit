using FluentAssertions;
using NUnit.Framework;
using Union.Playwright.NUnit.Core;
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
        var htmlPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Integration", "StackOverflow", "questions.html");
        var htmlContent = await File.ReadAllTextAsync(htmlPath);
        await Session.SO.MockQuestionsAsync(htmlContent);
    }

    [TearDown]
    public async Task TearDownRoute()
    {
        await Session.SO.UnmockQuestionsAsync();
    }

    [Test]
    public async Task QuestionsPage_WhenNavigated_StateIsResolved()
    {
        await Session.SO.Go.ToPage<QuestionsPage>();

        Session.SO.State.PageIs<QuestionsPage>().Should().BeTrue();
    }

    [Test]
    public async Task QuestionsPage_WhenLoaded_HasExpectedQuestionIds()
    {
        var page = await Session.SO.Go.ToPage<QuestionsPage>();

        var ids = await page.Questions.GetIdsAsync();

        ids.Should().HaveCount(3);
        ids.Should().Contain("79880001");
        ids.Should().Contain("79880002");
        ids.Should().Contain("79880003");
    }

    [Test]
    public async Task QuestionsPage_FirstItem_TitleIsVisible()
    {
        var page = await Session.SO.Go.ToPage<QuestionsPage>();
        var firstItem = await page.Questions.FindSingleAsync();
        firstItem.Should().NotBeNull();

        await Expect(firstItem!.Title).ToBeVisibleAsync();
    }

    [Test]
    public async Task QuestionsPage_FirstItem_HasTags()
    {
        var page = await Session.SO.Go.ToPage<QuestionsPage>();
        var firstItem = await page.Questions.FindSingleAsync();
        firstItem.Should().NotBeNull();

        var tagNames = await firstItem!.Tags.GetIdsAsync();
        tagNames.Should().NotBeEmpty();
        tagNames.Should().Contain("playwright");
        tagNames.Should().Contain("testing");
        tagNames.Should().Contain("csharp");
    }

    [Test]
    public async Task QuestionsPage_FirstItem_TagLinkIsVisible()
    {
        var page = await Session.SO.Go.ToPage<QuestionsPage>();
        var firstItem = await page.Questions.FindSingleAsync();
        firstItem.Should().NotBeNull();

        var firstTag = await firstItem!.Tags.FindSingleAsync();
        firstTag.Should().NotBeNull();

        await Expect(firstTag!.Link).ToBeVisibleAsync();
    }
}
