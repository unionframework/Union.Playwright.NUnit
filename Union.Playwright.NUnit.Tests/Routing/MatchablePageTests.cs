using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.NUnit.Pages;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Tests.Routing;

[TestFixture]
public class MatchablePageTests
{
    private IPage _mockPage = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPage = Substitute.For<IPage>();
        _mockPage.Url.Returns("https://example.com/test");
    }

    #region UriMatchResult.Reason Tests

    [Test]
    public void UriMatchResult_Matched_WithReason_SetsReasonProperty()
    {
        // Act
        var result = UriMatchResult.Matched("URL pattern matched");

        // Assert
        result.Success.Should().BeTrue();
        result.Reason.Should().Be("URL pattern matched");
    }

    [Test]
    public void UriMatchResult_Unmatched_WithReason_SetsReasonProperty()
    {
        // Act
        var result = UriMatchResult.Unmatched("DOM element not found");

        // Assert
        result.Success.Should().BeFalse();
        result.Reason.Should().Be("DOM element not found");
    }

    [Test]
    public void UriMatchResult_Matched_WithoutReason_ReasonIsNull()
    {
        // Act
        var result = UriMatchResult.Matched();

        // Assert
        result.Success.Should().BeTrue();
        result.Reason.Should().BeNull();
    }

    [Test]
    public void UriMatchResult_Constructor_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var data = new Dictionary<string, string> { ["id"] = "123" };
        var _params = new Dictionary<string, string> { ["tab"] = "details" };
        var cookies = new List<Cookie>();

        // Act
        var result = new UriMatchResult(true, data, _params, cookies, "Custom reason");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(data);
        result.Params.Should().BeEquivalentTo(_params);
        result.Cookies.Should().BeSameAs(cookies);
        result.Reason.Should().Be("Custom reason");
    }

    #endregion

    #region MatchablePage.MatchAsync Tests

    [Test]
    public async Task MatchAsync_DefaultImplementation_UsesUriMatcher()
    {
        // Arrange
        var page = new TestMatchablePage();
        var requestData = new RequestData("https://example.com/users/123");
        var baseUrlInfo = new BaseUrlInfo("example.com", "");

        // Act
        var result = await page.MatchAsync(requestData, baseUrlInfo, _mockPage);

        // Assert
        result.Success.Should().BeTrue();
        result.Data["userId"].Should().Be("123");
    }

    [Test]
    public async Task MatchAsync_UrlDoesNotMatch_ReturnsUnmatched()
    {
        // Arrange
        var page = new TestMatchablePage();
        var requestData = new RequestData("https://example.com/other/path");
        var baseUrlInfo = new BaseUrlInfo("example.com", "");

        // Act
        var result = await page.MatchAsync(requestData, baseUrlInfo, _mockPage);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task MatchAsync_WithDomCheck_CanVerifyElement()
    {
        // Arrange
        var mockLocator = Substitute.For<ILocator>();
        mockLocator.CountAsync().Returns(1);
        _mockPage.Locator(".test-element").Returns(mockLocator);

        var page = new TestMatchablePageWithDomCheck();
        var requestData = new RequestData("https://example.com/users/123");
        var baseUrlInfo = new BaseUrlInfo("example.com", "");

        // Act
        var result = await page.MatchAsync(requestData, baseUrlInfo, _mockPage);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Test]
    public async Task MatchAsync_WithDomCheck_ElementNotFound_ReturnsUnmatched()
    {
        // Arrange
        var mockLocator = Substitute.For<ILocator>();
        mockLocator.CountAsync().Returns(0);
        _mockPage.Locator(".test-element").Returns(mockLocator);

        var page = new TestMatchablePageWithDomCheck();
        var requestData = new RequestData("https://example.com/users/123");
        var baseUrlInfo = new BaseUrlInfo("example.com", "");

        // Act
        var result = await page.MatchAsync(requestData, baseUrlInfo, _mockPage);

        // Assert
        result.Success.Should().BeFalse();
        result.Reason.Should().Be("Required element .test-element not found");
    }

    #endregion

    #region MatchUrlRouter Tests

    [Test]
    public void RegisterPage_MatchablePage_AddedToRegistry()
    {
        // Arrange
        var router = new MatchUrlRouter();

        // Act
        router.RegisterPage<TestMatchablePage>();

        // Assert
        var types = router.GetPageTypes();
        types.Should().Contain(typeof(TestMatchablePage));
    }

    [Test]
    public void RegisterPage_RegularPage_AddedToRegistry()
    {
        // Arrange
        var router = new MatchUrlRouter();

        // Act
        router.RegisterPage<TestRegularPage>();

        // Assert
        var types = router.GetPageTypes();
        types.Should().Contain(typeof(TestRegularPage));
    }

    [Test]
    public void RegisterPage_DuplicatePath_RegularPages_ThrowsException()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<TestRegularPage>();

        // Act
        var act = () => router.RegisterPage<TestRegularPageDuplicate>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate page path*");
    }

    [Test]
    public async Task GetPageAsync_MatchablePageMatchesFirst_ReturnsMatchablePage()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<TestMatchablePageShared>();
        router.RegisterPage<TestRegularPageShared>();

        var requestData = new RequestData("https://example.com/shared");
        var baseUrlInfo = new BaseUrlInfo("example.com", "");

        // Act
        var result = await router.GetPageAsync(requestData, baseUrlInfo, _mockPage);

        // Assert
        result.Should().BeOfType<TestMatchablePageShared>();
    }

    [Test]
    public async Task GetPageAsync_MatchablePageFails_FallsBackToRegularPage()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<TestMatchablePageThatFails>();
        router.RegisterPage<TestRegularPage>();

        var requestData = new RequestData("https://example.com/test");
        var baseUrlInfo = new BaseUrlInfo("example.com", "");

        // Act
        var result = await router.GetPageAsync(requestData, baseUrlInfo, _mockPage);

        // Assert
        result.Should().BeOfType<TestRegularPage>();
    }

    [Test]
    public async Task GetPageAsync_NoMatch_ReturnsNull()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<TestRegularPage>();

        var requestData = new RequestData("https://example.com/nonexistent");
        var baseUrlInfo = new BaseUrlInfo("example.com", "");

        // Act
        var result = await router.GetPageAsync(requestData, baseUrlInfo, _mockPage);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetPageAsync_MatchablePageThrows_ExceptionPropagates()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<TestMatchablePageThatThrows>();

        var requestData = new RequestData("https://example.com/throws");
        var baseUrlInfo = new BaseUrlInfo("example.com", "");

        // Act
        var act = async () => await router.GetPageAsync(requestData, baseUrlInfo, _mockPage);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception from MatchAsync");
    }

    [Test]
    public async Task GetPageAsync_SetsLastMatchReason()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<TestRegularPage>();

        var requestData = new RequestData("https://example.com/test");
        var baseUrlInfo = new BaseUrlInfo("example.com", "");

        // Act
        var result = await router.GetPageAsync(requestData, baseUrlInfo, _mockPage);

        // Assert
        result.Should().NotBeNull();
        router.LastMatchReason.Should().Contain("TestRegularPage");
    }

    [Test]
    public void HasPage_MatchablePage_ReturnsTrue()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<TestMatchablePage>();
        var page = new TestMatchablePage();

        // Act & Assert
        router.HasPage(page).Should().BeTrue();
    }

    [Test]
    public void HasPage_RegularPage_ReturnsTrue()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<TestRegularPage>();
        var page = new TestRegularPage();

        // Act & Assert
        router.HasPage(page).Should().BeTrue();
    }

    #endregion

    #region Test Page Classes

    private class TestMatchablePage : MatchablePage
    {
        public override string AbsolutePath => "/users/{userId}";
    }

    private class TestMatchablePageWithDomCheck : MatchablePage
    {
        public override string AbsolutePath => "/users/{userId}";

        public override async ValueTask<UriMatchResult> MatchAsync(
            RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
        {
            var baseResult = await base.MatchAsync(requestData, baseUrlInfo, playwrightPage);
            if (!baseResult.Success)
                return baseResult;

            var locator = playwrightPage.Locator(".test-element");
            var count = await locator.CountAsync();
            if (count == 0)
                return UriMatchResult.Unmatched("Required element .test-element not found");

            return baseResult;
        }
    }

    private class TestMatchablePageShared : MatchablePage
    {
        public override string AbsolutePath => "/shared";
    }

    private class TestMatchablePageThatFails : MatchablePage
    {
        public override string AbsolutePath => "/test";

        public override ValueTask<UriMatchResult> MatchAsync(
            RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
        {
            return ValueTask.FromResult(UriMatchResult.Unmatched("Always fails"));
        }
    }

    private class TestMatchablePageThatThrows : MatchablePage
    {
        public override string AbsolutePath => "/throws";

        public override ValueTask<UriMatchResult> MatchAsync(
            RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
        {
            throw new InvalidOperationException("Test exception from MatchAsync");
        }
    }

    private class TestRegularPage : UnionPage
    {
        public override string AbsolutePath => "/test";
    }

    private class TestRegularPageDuplicate : UnionPage
    {
        public override string AbsolutePath => "/test";
    }

    private class TestRegularPageShared : UnionPage
    {
        public override string AbsolutePath => "/shared";
    }

    #endregion
}
