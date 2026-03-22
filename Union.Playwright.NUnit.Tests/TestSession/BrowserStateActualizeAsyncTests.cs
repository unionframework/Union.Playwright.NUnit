using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;
using Union.Playwright.NUnit.Services;
using Union.Playwright.NUnit.TestSession;

namespace Union.Playwright.NUnit.Tests.TestSession;

[TestFixture]
public class BrowserStateActualizeAsyncTests
{
    private IPageResolver _pageResolver = null!;
    private IUnionService _mockService = null!;
    private IPage _playwrightPage = null!;
    private BrowserState _browserState = null!;

    // Test URL that will match our base URL pattern
    private const string TestUrl = "https://test.example.com/users/123";
    private const string BaseUrl = "https://test.example.com";

    [SetUp]
    public void SetUp()
    {
        _pageResolver = Substitute.For<IPageResolver>();
        _mockService = Substitute.For<IUnionService>();
        _playwrightPage = Substitute.For<IPage>();
        _playwrightPage.Url.Returns(TestUrl);

        // Configure BaseUrlPattern to match test URLs
        var baseUrlPattern = new BaseUrlPattern(new BaseUrlRegexBuilder(BaseUrl).Build());
        _pageResolver.BaseUrlPattern.Returns(baseUrlPattern);

        _browserState = new BrowserState(_pageResolver, _mockService);
    }

    [Test]
    public async Task ActualizeAsync_MatchFound_SetsPageProperty()
    {
        // Arrange
        var mockPage = Substitute.For<IUnionPage>();
        _pageResolver.GetPageAsync(
            Arg.Any<RequestData>(),
            Arg.Any<BaseUrlInfo>(),
            Arg.Any<IPage>())
            .Returns(ValueTask.FromResult<IUnionPage?>(mockPage));

        // Act
        await _browserState.ActualizeAsync(_playwrightPage);

        // Assert
        _browserState.Page.Should().BeSameAs(mockPage);
    }

    [Test]
    public async Task ActualizeAsync_NoMatch_SetsPageToNull()
    {
        // Arrange
        _pageResolver.GetPageAsync(
            Arg.Any<RequestData>(),
            Arg.Any<BaseUrlInfo>(),
            Arg.Any<IPage>())
            .Returns(ValueTask.FromResult<IUnionPage?>(null));

        // Act
        await _browserState.ActualizeAsync(_playwrightPage);

        // Assert
        _browserState.Page.Should().BeNull();
    }

    [Test]
    public async Task ActualizeAsync_MatchFound_CallsActivate()
    {
        // Arrange
        var mockPage = Substitute.For<IUnionPage>();
        _pageResolver.GetPageAsync(
            Arg.Any<RequestData>(),
            Arg.Any<BaseUrlInfo>(),
            Arg.Any<IPage>())
            .Returns(ValueTask.FromResult<IUnionPage?>(mockPage));

        // Act
        await _browserState.ActualizeAsync(_playwrightPage);

        // Assert
        mockPage.Received(1).Activate(_playwrightPage, _mockService);
    }

    [Test]
    public async Task ActualizeAsync_Always_SetsLastActualizedUrl()
    {
        // Arrange
        _pageResolver.GetPageAsync(
            Arg.Any<RequestData>(),
            Arg.Any<BaseUrlInfo>(),
            Arg.Any<IPage>())
            .Returns(ValueTask.FromResult<IUnionPage?>(null));

        // Act
        await _browserState.ActualizeAsync(_playwrightPage);

        // Assert
        _browserState.LastActualizedUrl.Should().Be(TestUrl);
    }

    [Test]
    public async Task ActualizeAsync_MatchFound_SetsDiagnosticMessageWithPageTypeName()
    {
        // Arrange
        var mockPage = Substitute.For<IUnionPage>();
        _pageResolver.GetPageAsync(
            Arg.Any<RequestData>(),
            Arg.Any<BaseUrlInfo>(),
            Arg.Any<IPage>())
            .Returns(ValueTask.FromResult<IUnionPage?>(mockPage));

        // Act
        await _browserState.ActualizeAsync(_playwrightPage);

        // Assert
        _browserState.LastDiagnosticMessage.Should().Contain("Resolved to");
    }

    [Test]
    public async Task ActualizeAsync_NoMatch_SetsDiagnosticMessageIndicatingNoMatch()
    {
        // Arrange
        _pageResolver.GetPageAsync(
            Arg.Any<RequestData>(),
            Arg.Any<BaseUrlInfo>(),
            Arg.Any<IPage>())
            .Returns(ValueTask.FromResult<IUnionPage?>(null));

        // Act
        await _browserState.ActualizeAsync(_playwrightPage);

        // Assert
        _browserState.LastDiagnosticMessage.Should().Contain("no page pattern matched");
    }

    [Test]
    public async Task ActualizeAsync_BaseUrlDoesNotMatch_SetsDiagnosticMessageIndicatingNoBaseMatch()
    {
        // Arrange: Use a URL that won't match the base URL pattern
        var nonMatchingPage = Substitute.For<IPage>();
        nonMatchingPage.Url.Returns("https://other-site.com/page");

        // Act
        await _browserState.ActualizeAsync(nonMatchingPage);

        // Assert
        _browserState.LastDiagnosticMessage.Should().Contain("Base URL did not match");
        _browserState.Page.Should().BeNull();
    }

    [Test]
    public async Task ActualizeAsync_WithMatchablePage_UsesAsyncMatching()
    {
        // Arrange
        var mockPage = Substitute.For<IUnionPage>();
        _pageResolver.GetPageAsync(
            Arg.Any<RequestData>(),
            Arg.Any<BaseUrlInfo>(),
            Arg.Any<IPage>())
            .Returns(ValueTask.FromResult<IUnionPage?>(mockPage));

        // Act
        await _browserState.ActualizeAsync(_playwrightPage);

        // Assert: Verify GetPageAsync was called (not the sync GetPage)
        await _pageResolver.Received(1).GetPageAsync(
            Arg.Any<RequestData>(),
            Arg.Any<BaseUrlInfo>(),
            _playwrightPage);
    }

    [Test]
    public async Task ActualizeAsync_ClearsPageBeforeResolving()
    {
        // Arrange: First actualize to set Page
        var firstPage = Substitute.For<IUnionPage>();
        _pageResolver.GetPageAsync(
            Arg.Any<RequestData>(),
            Arg.Any<BaseUrlInfo>(),
            Arg.Any<IPage>())
            .Returns(ValueTask.FromResult<IUnionPage?>(firstPage));
        await _browserState.ActualizeAsync(_playwrightPage);

        // Now configure to return null
        _pageResolver.GetPageAsync(
            Arg.Any<RequestData>(),
            Arg.Any<BaseUrlInfo>(),
            Arg.Any<IPage>())
            .Returns(ValueTask.FromResult<IUnionPage?>(null));

        // Act: Second actualize should clear the page
        await _browserState.ActualizeAsync(_playwrightPage);

        // Assert
        _browserState.Page.Should().BeNull();
    }

    [TestCase(true, "Resolved to")]
    [TestCase(false, "no page pattern matched")]
    public async Task ActualizeAsync_SetsAppropriateDiagnosticMessage(
        bool matchFound,
        string expectedMessageContains)
    {
        // Arrange
        IUnionPage? resolvedPage = matchFound ? Substitute.For<IUnionPage>() : null;
        _pageResolver.GetPageAsync(
            Arg.Any<RequestData>(),
            Arg.Any<BaseUrlInfo>(),
            Arg.Any<IPage>())
            .Returns(ValueTask.FromResult(resolvedPage));

        // Act
        await _browserState.ActualizeAsync(_playwrightPage);

        // Assert
        _browserState.LastDiagnosticMessage.Should().Contain(expectedMessageContains);
    }

    [Test]
    public async Task ActualizeAsync_PassesCorrectRequestDataToResolver()
    {
        // Arrange
        RequestData? capturedRequestData = null;
        _pageResolver.GetPageAsync(
            Arg.Do<RequestData>(rd => capturedRequestData = rd),
            Arg.Any<BaseUrlInfo>(),
            Arg.Any<IPage>())
            .Returns(ValueTask.FromResult<IUnionPage?>(null));

        // Act
        await _browserState.ActualizeAsync(_playwrightPage);

        // Assert
        capturedRequestData.Should().NotBeNull();
        capturedRequestData!.Url.Should().Be(TestUrl);
    }

    [Test]
    public async Task ActualizeAsync_PassesPlaywrightPageToResolver()
    {
        // Arrange
        IPage? capturedPage = null;
        _pageResolver.GetPageAsync(
            Arg.Any<RequestData>(),
            Arg.Any<BaseUrlInfo>(),
            Arg.Do<IPage>(p => capturedPage = p))
            .Returns(ValueTask.FromResult<IUnionPage?>(null));

        // Act
        await _browserState.ActualizeAsync(_playwrightPage);

        // Assert
        capturedPage.Should().BeSameAs(_playwrightPage);
    }
}
