using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Services;
using Union.Playwright.NUnit.TestSession;
using Union.Playwright.NUnit.Tests.Fakes;

namespace Union.Playwright.NUnit.Tests.TestSession;

/// <summary>
/// Tests that verify thread-safety behavior during parallel test execution.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
[Category("Concurrency")]
[Category("ThreadSafety")]
public class ConcurrencyTests
{
    private IBrowserContext CreateFakeContext()
    {
        var mockContext = Substitute.For<IBrowserContext>();
        var mockPage = Substitute.For<IPage>();
        mockContext.NewPageAsync().Returns(Task.FromResult(mockPage));
        return mockContext;
    }

    #region TestSessionProvider Concurrency Tests

    [Test]
    [Repeat(10)]
    [Description("Verifies CreateTestSession doesn't throw when called from multiple threads")]
    public async Task CreateTestSession_WhenCalledConcurrently_DoesNotThrow()
    {
        // Arrange
        var provider = new TestableTestSessionProvider();
        var exceptions = new ConcurrentBag<Exception>();
        var scopedSessions = new ConcurrentBag<ScopedTestSession>();

        // Act - call CreateTestSession concurrently from multiple threads
        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            try
            {
                var context = CreateFakeContext();
                var scoped = provider.CreateTestSession(context);
                scopedSessions.Add(scoped);
                return scoped;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                return null;
            }
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty("concurrent access should not throw exceptions");
        results.Should().AllSatisfy(r => r.Should().NotBeNull());

        // Cleanup
        foreach (var s in scopedSessions)
        {
            if (s != null) await s.DisposeAsync();
        }
    }

    [Test]
    [Repeat(10)]
    [Description("Verifies concurrent CreateTestSession calls produce distinct sessions")]
    public async Task CreateTestSession_WhenCalledConcurrently_ProducesDistinctSessions()
    {
        // Arrange
        var provider = new TestableTestSessionProvider();
        var scopedSessions = new ConcurrentBag<ScopedTestSession>();

        // Act
        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            var context = CreateFakeContext();
            var scoped = provider.CreateTestSession(context);
            scopedSessions.Add(scoped);
            return scoped;
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        var sessions = results.Select(r => r!.Session).ToList();
        sessions.Should().OnlyHaveUniqueItems("each call should create a distinct session");

        // Cleanup
        foreach (var s in scopedSessions)
        {
            if (s != null) await s.DisposeAsync();
        }
    }

    #endregion

    #region AsyncLocal Isolation Tests

    [Test]
    [Description("Verifies AsyncLocal properly isolates sessions between async flows")]
    public async Task AsyncLocal_IsolatesBetweenAsyncFlows()
    {
        // Arrange
        ScopedTestSession? session1Observed = null;
        ScopedTestSession? session2Observed = null;

        var provider = new TestableTestSessionProvider();
        var context1 = CreateFakeContext();
        var context2 = CreateFakeContext();

        var session1 = provider.CreateTestSession(context1);
        var session2 = provider.CreateTestSession(context2);

        // Act - Set different sessions in different async flows
        var task1 = Task.Run(async () =>
        {
            ScopedTestSession.SetCurrent(session1);
            await Task.Delay(50);
            session1Observed = ScopedTestSession.Current;
        });

        var task2 = Task.Run(async () =>
        {
            ScopedTestSession.SetCurrent(session2);
            await Task.Delay(50);
            session2Observed = ScopedTestSession.Current;
        });

        await Task.WhenAll(task1, task2);

        // Assert
        session1Observed.Should().BeSameAs(session1, "task1 should see session1");
        session2Observed.Should().BeSameAs(session2, "task2 should see session2");

        // Cleanup
        ScopedTestSession.SetCurrent(null);
        await session1.DisposeAsync();
        await session2.DisposeAsync();
    }

    #endregion
}
