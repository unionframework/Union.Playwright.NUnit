using System.Collections.Concurrent;
using FluentAssertions;
using NUnit.Framework;
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
                var scoped = provider.CreateTestSession();
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
            var scoped = provider.CreateTestSession();
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
}
