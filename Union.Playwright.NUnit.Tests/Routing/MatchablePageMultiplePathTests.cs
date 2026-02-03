using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.NUnit.Pages;
using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Tests.Routing;

[TestFixture]
public class MatchablePageMultiplePathTests
{
    private IPage _mockPage = null!;
    private RequestData _requestData = null!;
    private BaseUrlInfo _baseUrlInfo = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPage = Substitute.For<IPage>();
        _mockPage.Url.Returns("https://example.com/login");
        _requestData = new RequestData("https://example.com/login");
        _baseUrlInfo = new BaseUrlInfo("example.com", "");
    }

    #region Multiple MatchablePages with Same Path Tests

    [Test]
    public async Task GetPageAsync_TwoMatchablePagesWithSamePath_FirstRegisteredWins()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<FirstMatchablePage>();   // /login, always matches
        router.RegisterPage<SecondMatchablePage>();  // /login, always matches

        // Act
        var result = await router.GetPageAsync(_requestData, _baseUrlInfo, _mockPage);

        // Assert
        result.Should().BeOfType<FirstMatchablePage>();
    }

    [Test]
    public async Task GetPageAsync_FirstMatchablePageDeclines_SecondMatchablePageMatches()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<DecliningMatchablePage>();  // /login, always declines
        router.RegisterPage<AcceptingMatchablePage>();  // /login, always accepts

        // Act
        var result = await router.GetPageAsync(_requestData, _baseUrlInfo, _mockPage);

        // Assert
        result.Should().BeOfType<AcceptingMatchablePage>();
    }

    [Test]
    public async Task GetPageAsync_RegistrationOrderDeterminesPriority()
    {
        // Arrange
        var router1 = new MatchUrlRouter();
        router1.RegisterPage<PageA>();  // First
        router1.RegisterPage<PageB>();  // Second

        var router2 = new MatchUrlRouter();
        router2.RegisterPage<PageB>();  // First
        router2.RegisterPage<PageA>();  // Second

        // Act
        var result1 = await router1.GetPageAsync(_requestData, _baseUrlInfo, _mockPage);
        var result2 = await router2.GetPageAsync(_requestData, _baseUrlInfo, _mockPage);

        // Assert
        result1.Should().BeOfType<PageA>();
        result2.Should().BeOfType<PageB>();
    }

    [Test]
    public async Task GetPageAsync_AllMatchablePagesDecline_FallsBackToRegularPage()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<DecliningMatchablePage>();  // /login, always declines
        router.RegisterPage<AnotherDecliningMatchablePage>();  // /login, always declines
        router.RegisterPage<RegularLoginPage>();  // /login, regular page

        // Act
        var result = await router.GetPageAsync(_requestData, _baseUrlInfo, _mockPage);

        // Assert
        result.Should().BeOfType<RegularLoginPage>();
    }

    [Test]
    public async Task GetPageAsync_AllMatchablePagesDecline_NoRegularPage_ReturnsNull()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<DecliningMatchablePage>();  // /login, always declines
        router.RegisterPage<AnotherDecliningMatchablePage>();  // /login, always declines

        // Act
        var result = await router.GetPageAsync(_requestData, _baseUrlInfo, _mockPage);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void RegisterPage_DuplicateMatchablePaths_SetsWarning()
    {
        // Arrange
        var router = new MatchUrlRouter();
        router.RegisterPage<FirstMatchablePage>();

        // Act
        router.RegisterPage<SecondMatchablePage>();

        // Assert
        router.LastRegistrationWarning.Should().NotBeNull();
        router.LastRegistrationWarning.Should().Contain("same path");
        router.LastRegistrationWarning.Should().Contain("/login");
    }

    #endregion

    #region Test Page Classes

    public class FirstMatchablePage : MatchablePage
    {
        public override string AbsolutePath => "/login";

        public override async ValueTask<UriMatchResult> MatchAsync(
            RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
        {
            var baseMatch = await base.MatchAsync(requestData, baseUrlInfo, playwrightPage);
            return baseMatch; // Always matches if URL matches
        }
    }

    public class SecondMatchablePage : MatchablePage
    {
        public override string AbsolutePath => "/login";

        public override async ValueTask<UriMatchResult> MatchAsync(
            RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
        {
            var baseMatch = await base.MatchAsync(requestData, baseUrlInfo, playwrightPage);
            return baseMatch; // Always matches if URL matches
        }
    }

    public class DecliningMatchablePage : MatchablePage
    {
        public override string AbsolutePath => "/login";

        public override ValueTask<UriMatchResult> MatchAsync(
            RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
        {
            return ValueTask.FromResult(UriMatchResult.Unmatched("Declining intentionally"));
        }
    }

    public class AnotherDecliningMatchablePage : MatchablePage
    {
        public override string AbsolutePath => "/login";

        public override ValueTask<UriMatchResult> MatchAsync(
            RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
        {
            return ValueTask.FromResult(UriMatchResult.Unmatched("Also declining intentionally"));
        }
    }

    public class AcceptingMatchablePage : MatchablePage
    {
        public override string AbsolutePath => "/login";

        public override async ValueTask<UriMatchResult> MatchAsync(
            RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
        {
            var baseMatch = await base.MatchAsync(requestData, baseUrlInfo, playwrightPage);
            return baseMatch; // Always matches if URL matches
        }
    }

    public class PageA : MatchablePage
    {
        public override string AbsolutePath => "/login";

        public override async ValueTask<UriMatchResult> MatchAsync(
            RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
        {
            var baseMatch = await base.MatchAsync(requestData, baseUrlInfo, playwrightPage);
            return baseMatch;
        }
    }

    public class PageB : MatchablePage
    {
        public override string AbsolutePath => "/login";

        public override async ValueTask<UriMatchResult> MatchAsync(
            RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
        {
            var baseMatch = await base.MatchAsync(requestData, baseUrlInfo, playwrightPage);
            return baseMatch;
        }
    }

    public class RegularLoginPage : UnionPage
    {
        public override string AbsolutePath => "/login";
    }

    #endregion
}
