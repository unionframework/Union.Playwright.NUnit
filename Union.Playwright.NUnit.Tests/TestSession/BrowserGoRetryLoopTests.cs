using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;
using Union.Playwright.NUnit.Services;
using Union.Playwright.NUnit.TestSession;

namespace Union.Playwright.NUnit.Tests.TestSession;

/// <summary>
/// Tests for BrowserGo's retry loop behavior during navigation.
/// These tests verify the AfterNavigateAsync retry logic indirectly through public navigation methods.
/// </summary>
[TestFixture]
public class BrowserGoRetryLoopTests
{
    private IUnionService _mockService = null!;
    private IBrowserState _mockState = null!;
    private IPage _mockPage = null!;

    [SetUp]
    public void SetUp()
    {
        _mockService = Substitute.For<IUnionService>();
        _mockState = Substitute.For<IBrowserState>();
        _mockPage = Substitute.For<IPage>();

        // Default setup: service returns mock page
        _mockService.GetOrCreatePageAsync().Returns(Task.FromResult(_mockPage));

        // Default: GetRequestData returns a valid RequestData
        _mockService.GetRequestData(Arg.Any<IUnionPage>())
            .Returns(new RequestData("https://test.example.com/page"));
    }

    #region Immediate Resolution Tests

    [Test]
    public async Task ToUrl_PageResolvesImmediately_ActualizeAsyncCalledOnce()
    {
        // Arrange: Mock state that returns resolved page immediately
        var resolvedPage = Substitute.For<IUnionPage>();
        resolvedPage.WaitLoadedAsync().Returns(Task.CompletedTask);

        _mockState.PageIs<IUnionPage>().Returns(true);
        _mockState.PageAs<IUnionPage>().Returns(resolvedPage);

        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = 5000,
            NavigationPollIntervalMs = 100
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);

        // Act
        await browserGo.ToUrl("https://test.example.com/page");

        // Assert: ActualizeAsync called only once (no retries needed)
        await _mockState.Received(1).ActualizeAsync(_mockPage);
    }

    #endregion

    #region Timeout Behavior Tests

    [Test]
    public async Task ToUrl_PageNeverResolves_StopsAfterTimeout()
    {
        // Arrange: Mock state that never resolves (PageIs always returns false)
        _mockState.PageIs<IUnionPage>().Returns(false);

        var timeoutMs = 500;
        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = timeoutMs,
            NavigationPollIntervalMs = 100
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await browserGo.ToUrl("https://test.example.com/page");

        stopwatch.Stop();

        // Assert: Method returns within timeout + reasonable margin (poll interval + buffer)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(timeoutMs + 300,
            "navigation should complete within timeout plus margin");

        // Assert: Multiple ActualizeAsync calls were made (initial + retries)
        var calls = _mockState.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == "ActualizeAsync");
        calls.Should().BeGreaterThan(1, "should have made retry attempts");
    }

    [Test]
    public async Task ToUrl_TimeoutIsZero_NoRetryLoop()
    {
        // Arrange: Mock state that never resolves, set timeout to 0
        _mockState.PageIs<IUnionPage>().Returns(false);

        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = 0,
            NavigationPollIntervalMs = 100
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);

        // Act
        await browserGo.ToUrl("https://test.example.com/page");

        // Assert: ActualizeAsync called only once (no retry loop when timeout is 0)
        await _mockState.Received(1).ActualizeAsync(_mockPage);
    }

    #endregion

    #region Poll Interval Tests

    [Test]
    public async Task ToUrl_RetriesAtConfiguredInterval()
    {
        // Arrange: Set poll interval to 100ms, timeout to 350ms
        // Expected: Initial call + approximately 3 retries (at 100ms, 200ms, 300ms)
        _mockState.PageIs<IUnionPage>().Returns(false);

        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = 350,
            NavigationPollIntervalMs = 100
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);

        // Act
        await browserGo.ToUrl("https://test.example.com/page");

        // Assert: Approximately 3-4 ActualizeAsync calls made (initial + retries)
        var calls = _mockState.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == "ActualizeAsync");

        // Initial call (1) + retries during 350ms with 100ms interval
        // Should be approximately 3-4 calls total
        calls.Should().BeInRange(3, 5,
            "should make initial call plus 2-4 retry attempts in 350ms with 100ms interval");
    }

    #endregion

    #region WaitLoadedAsync Tests

    [Test]
    public async Task ToUrl_PageResolved_CallsWaitLoadedAsync()
    {
        // Arrange: Mock resolved page with WaitLoadedAsync
        var resolvedPage = Substitute.For<IUnionPage>();
        resolvedPage.WaitLoadedAsync().Returns(Task.CompletedTask);

        _mockState.PageIs<IUnionPage>().Returns(true);
        _mockState.PageAs<IUnionPage>().Returns(resolvedPage);

        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = 5000,
            WaitLoadedTimeoutMs = 30000
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);

        // Act
        await browserGo.ToUrl("https://test.example.com/page");

        // Assert: WaitLoadedAsync was called on resolved page
        await resolvedPage.Received(1).WaitLoadedAsync();
    }

    [Test]
    public async Task ToUrl_WaitLoadedAsyncTimesOut_ThrowsTimeoutException()
    {
        // Arrange: Mock WaitLoadedAsync that takes longer than timeout
        var resolvedPage = Substitute.For<IUnionPage>();

        // WaitLoadedAsync delays longer than the timeout
        resolvedPage.WaitLoadedAsync().Returns(async _ =>
        {
            await Task.Delay(5000); // Much longer than timeout
        });

        _mockState.PageIs<IUnionPage>().Returns(true);
        _mockState.PageAs<IUnionPage>().Returns(resolvedPage);

        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = 0,
            WaitLoadedTimeoutMs = 100 // Short timeout
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);

        // Act & Assert
        var act = async () => await browserGo.ToUrl("https://test.example.com/page");

        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("*WaitLoadedAsync*timed out*100ms*");
    }

    [Test]
    public async Task ToUrl_WaitLoadedTimeoutIsZero_NoTimeoutProtection()
    {
        // Arrange: WaitLoadedAsync completes quickly, timeout is disabled
        var resolvedPage = Substitute.For<IUnionPage>();
        resolvedPage.WaitLoadedAsync().Returns(Task.CompletedTask);

        _mockState.PageIs<IUnionPage>().Returns(true);
        _mockState.PageAs<IUnionPage>().Returns(resolvedPage);

        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = 0,
            WaitLoadedTimeoutMs = 0 // Disabled
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);

        // Act
        await browserGo.ToUrl("https://test.example.com/page");

        // Assert: WaitLoadedAsync still called (just without timeout wrapper)
        await resolvedPage.Received(1).WaitLoadedAsync();
    }

    #endregion

    #region Diagnostic Message Tests

    [Test]
    public async Task ToUrl_TimesOut_UpdatesDiagnosticMessage()
    {
        // Arrange: Mock state that never resolves
        _mockState.PageIs<IUnionPage>().Returns(false);

        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = 200,
            NavigationPollIntervalMs = 50
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);

        // Act
        await browserGo.ToUrl("https://test.example.com/page");

        // Assert: AppendDiagnosticMessage was called with timeout info
        _mockState.Received().AppendDiagnosticMessage(
            Arg.Is<string>(s => s.Contains("timed out") && s.Contains("attempts")));
    }

    [Test]
    public async Task ToUrl_PageResolvesEventually_NoDiagnosticMessageAppended()
    {
        // Arrange: Page doesn't resolve initially, but resolves after a couple retries
        var callCount = 0;
        var resolvedPage = Substitute.For<IUnionPage>();
        resolvedPage.WaitLoadedAsync().Returns(Task.CompletedTask);

        _mockState.PageIs<IUnionPage>().Returns(_ =>
        {
            callCount++;
            return callCount >= 3; // Resolve on 3rd check
        });
        _mockState.PageAs<IUnionPage>().Returns(resolvedPage);

        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = 5000,
            NavigationPollIntervalMs = 50
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);

        // Act
        await browserGo.ToUrl("https://test.example.com/page");

        // Assert: AppendDiagnosticMessage was NOT called (page resolved before timeout)
        _mockState.DidNotReceive().AppendDiagnosticMessage(Arg.Any<string>());
    }

    #endregion

    #region Navigation Method Variations

    [Test]
    public async Task Refresh_TriggersRetryLoopSameAsToUrl()
    {
        // Arrange
        _mockState.PageIs<IUnionPage>().Returns(false);

        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = 200,
            NavigationPollIntervalMs = 50
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);

        // Act
        await browserGo.Refresh();

        // Assert: Multiple ActualizeAsync calls (retry loop triggered)
        var calls = _mockState.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == "ActualizeAsync");
        calls.Should().BeGreaterThan(1, "Refresh should trigger retry loop");
    }

    [Test]
    public async Task Back_TriggersRetryLoopSameAsToUrl()
    {
        // Arrange
        _mockState.PageIs<IUnionPage>().Returns(false);

        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = 200,
            NavigationPollIntervalMs = 50
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);

        // Act
        await browserGo.Back();

        // Assert: Multiple ActualizeAsync calls (retry loop triggered)
        var calls = _mockState.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == "ActualizeAsync");
        calls.Should().BeGreaterThan(1, "Back should trigger retry loop");
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task ToUrl_InitialActualizeAsyncThrows_PropagatesException()
    {
        // Arrange: First ActualizeAsync throws
        _mockState.ActualizeAsync(_mockPage)
            .Returns(new ValueTask(Task.FromException(new InvalidOperationException("Test exception"))));

        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = 5000
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);

        // Act & Assert
        var act = async () => await browserGo.ToUrl("https://test.example.com/page");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*actualize*");
    }

    [Test]
    public async Task ToUrl_RetryActualizeAsyncThrows_ContinuesRetrying()
    {
        // Arrange: First call succeeds but doesn't resolve, retry calls throw
        var callCount = 0;
        var resolvedPage = Substitute.For<IUnionPage>();
        resolvedPage.WaitLoadedAsync().Returns(Task.CompletedTask);

        _mockState.ActualizeAsync(_mockPage).Returns(_ =>
        {
            callCount++;
            if (callCount == 1)
            {
                // First call succeeds but page not resolved
                return ValueTask.CompletedTask;
            }
            else if (callCount < 4)
            {
                // Retry calls 2-3 throw
                return new ValueTask(Task.FromException(new Exception("Transient error")));
            }
            // Call 4+ succeeds
            return ValueTask.CompletedTask;
        });

        // Page resolves on 4th attempt
        _mockState.PageIs<IUnionPage>().Returns(_ => callCount >= 4);
        _mockState.PageAs<IUnionPage>().Returns(_ => callCount >= 4 ? resolvedPage : null);

        var settings = new TestSettings
        {
            NavigationResolveTimeoutMs = 5000,
            NavigationPollIntervalMs = 50
        };

        var browserGo = new BrowserGo(_mockService, _mockState, settings);

        // Act - should not throw despite retry exceptions
        await browserGo.ToUrl("https://test.example.com/page");

        // Assert: Multiple calls were made (retries continued despite exceptions)
        callCount.Should().BeGreaterThanOrEqualTo(4,
            "should continue retrying despite transient exceptions");
    }

    #endregion
}
