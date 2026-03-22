using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.TestSession;
using Union.Playwright.NUnit.Tests.Fakes;

namespace Union.Playwright.NUnit.Tests.TestSession;

[TestFixture]
public class TestSessionProviderTests
{
    private TestableTestSessionProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new TestableTestSessionProvider();
    }

    #region CreateTestSession (no-arg) Tests

    [Test]
    public void CreateTestSession_NoArg_ReturnsNonNullScopedSession()
    {
        // Act
        var scoped = _provider.CreateTestSession();

        // Assert
        scoped.Should().NotBeNull();
        scoped.Session.Should().NotBeNull();
        scoped.Session.Should().BeOfType<FakeTestSession>();
        scoped.HasContext.Should().BeFalse();
    }

    [Test]
    public void CreateTestSession_NoArg_CalledTwice_ReturnsDifferentSessions()
    {
        // Act
        var scoped1 = _provider.CreateTestSession();
        var scoped2 = _provider.CreateTestSession();

        // Assert
        scoped1.Session.Should().NotBeSameAs(scoped2.Session);
    }

    [Test]
    public void CreateTestSession_NoArg_SessionContainsRegisteredServices()
    {
        // Act
        var scoped = _provider.CreateTestSession();
        var services = scoped.Session.GetServices();

        // Assert
        services.Should().NotBeNull();
    }

    [Test]
    public async Task CreateTestSession_NoArg_ThenSetContext_ContextAccessible()
    {
        // Arrange
        var mockContext = Substitute.For<IBrowserContext>();

        // Act
        var scoped = _provider.CreateTestSession();
        scoped.SetContext(mockContext);

        // Assert
        scoped.Context.Should().BeSameAs(mockContext);

        // Cleanup
        await scoped.DisposeAsync();
    }

    [Test]
    public async Task CreateTestSession_NoArg_DisposingWithoutContext_DoesNotThrow()
    {
        // Act
        var scoped = _provider.CreateTestSession();

        // Assert
        var act = async () => await scoped.DisposeAsync();
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task CreateTestSession_NoArg_MultipleCalls_EachGetsIsolatedSession()
    {
        // Arrange & Act
        var sessions = Enumerable.Range(0, 5)
            .Select(_ => _provider.CreateTestSession())
            .ToList();

        try
        {
            // Assert
            var sessionInstances = sessions.Select(s => s.Session).ToList();
            sessionInstances.Should().OnlyHaveUniqueItems("each call should create a unique session");
        }
        finally
        {
            foreach (var s in sessions)
            {
                await s.DisposeAsync();
            }
        }
    }

    #endregion
}
