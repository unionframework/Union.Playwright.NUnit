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
    private IBrowserContext _fakeContext = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeContext = Substitute.For<IBrowserContext>();
        var fakePage = Substitute.For<IPage>();
        _fakeContext.NewPageAsync().Returns(Task.FromResult(fakePage));
        _provider = new TestableTestSessionProvider();
    }

    #region CreateTestSession Tests

    [Test]
    public async Task CreateTestSession_ReturnsNonNullScopedSession()
    {
        // Act
        await using var scoped = _provider.CreateTestSession(_fakeContext);

        // Assert
        scoped.Should().NotBeNull();
        scoped.Session.Should().NotBeNull();
        scoped.Session.Should().BeOfType<FakeTestSession>();
    }

    [Test]
    public async Task CreateTestSession_CalledTwice_ReturnsDifferentSessions()
    {
        // Arrange
        var context1 = Substitute.For<IBrowserContext>();
        var context2 = Substitute.For<IBrowserContext>();

        // Act
        await using var scoped1 = _provider.CreateTestSession(context1);
        await using var scoped2 = _provider.CreateTestSession(context2);

        // Assert
        scoped1.Session.Should().NotBeSameAs(scoped2.Session);
    }

    [Test]
    public async Task CreateTestSession_SessionContainsRegisteredServices()
    {
        // Act
        await using var scoped = _provider.CreateTestSession(_fakeContext);
        var services = scoped.Session.GetServices();

        // Assert
        services.Should().NotBeNull();
    }

    [Test]
    public async Task CreateTestSession_DisposingScope_DoesNotThrow()
    {
        // Act
        var scoped = _provider.CreateTestSession(_fakeContext);

        // Assert
        var act = async () => await scoped.DisposeAsync();
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task CreateTestSession_StoresContext()
    {
        // Act
        await using var scoped = _provider.CreateTestSession(_fakeContext);

        // Assert
        scoped.Context.Should().BeSameAs(_fakeContext);
    }

    #endregion

    #region Session Lifecycle Tests

    [Test]
    public async Task CreateTestSession_MultipleCalls_EachGetsIsolatedSession()
    {
        // Arrange
        var contexts = Enumerable.Range(0, 5)
            .Select(_ => Substitute.For<IBrowserContext>())
            .ToList();

        // Act
        var sessions = contexts
            .Select(ctx => _provider.CreateTestSession(ctx))
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
