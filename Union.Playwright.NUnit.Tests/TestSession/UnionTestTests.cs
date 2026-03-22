using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Services;
using Union.Playwright.NUnit.TestSession;

namespace Union.Playwright.NUnit.Tests.TestSession;

#region Test Doubles

/// <summary>
/// Minimal test session for verifying UnionTest DI wiring.
/// </summary>
public class StubTestSession : ITestSession
{
    private readonly List<IUnionService> _services;

    public StubTestSession(IEnumerable<IUnionService> services)
    {
        _services = services.ToList();
    }

    public List<IUnionService> GetServices()
    {
        return _services;
    }
}

/// <summary>
/// A TestSessionProvider for StubTestSession.
/// </summary>
public class StubTestSessionProvider : TestSessionProvider<StubTestSession>
{
    private static Action<IServiceCollection>? _pendingConfigurator;

    public StubTestSessionProvider() : this(null) { }

    public StubTestSessionProvider(Action<IServiceCollection>? configurator)
    {
        _ = configurator;
    }

    public static StubTestSessionProvider CreateWithServices(Action<IServiceCollection> configurator)
    {
        _pendingConfigurator = configurator;
        var provider = new StubTestSessionProvider(configurator);
        _pendingConfigurator = null;
        return provider;
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        if (_pendingConfigurator != null)
        {
            _pendingConfigurator.Invoke(services);
        }
        else
        {
            services.AddSingleton<IEnumerable<IUnionService>>(Array.Empty<IUnionService>());
        }
    }
}

#endregion

[TestFixture]
public class UnionTestTests
{
    #region ScopedTestSession Lifecycle Tests

    [Test]
    public void ScopedTestSession_CreatedWithoutContext_SessionIsAccessible()
    {
        // Arrange
        var mockSession = Substitute.For<ITestSession>();
        var scope = new ServiceCollection().BuildServiceProvider().CreateAsyncScope();

        // Act
        var scoped = new ScopedTestSession(mockSession, scope);

        // Assert
        scoped.Session.Should().BeSameAs(mockSession);
    }

    [Test]
    public void ScopedTestSession_CreatedWithoutContext_ContextThrows()
    {
        // Arrange
        var mockSession = Substitute.For<ITestSession>();
        var scope = new ServiceCollection().BuildServiceProvider().CreateAsyncScope();
        var scoped = new ScopedTestSession(mockSession, scope);

        // Act
        var act = () => scoped.Context;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Browser context is not yet available*");
    }

    [Test]
    public void ScopedTestSession_HasContext_IsFalseBeforeSetContext()
    {
        // Arrange
        var mockSession = Substitute.For<ITestSession>();
        var scope = new ServiceCollection().BuildServiceProvider().CreateAsyncScope();
        var scoped = new ScopedTestSession(mockSession, scope);

        // Assert
        scoped.HasContext.Should().BeFalse();
    }

    [Test]
    public void ScopedTestSession_SetContext_MakesContextAccessible()
    {
        // Arrange
        var mockSession = Substitute.For<ITestSession>();
        var scope = new ServiceCollection().BuildServiceProvider().CreateAsyncScope();
        var mockContext = Substitute.For<IBrowserContext>();
        var scoped = new ScopedTestSession(mockSession, scope);

        // Act
        scoped.SetContext(mockContext);

        // Assert
        scoped.Context.Should().BeSameAs(mockContext);
        scoped.HasContext.Should().BeTrue();
    }

    [Test]
    public void ScopedTestSession_SetContext_CalledTwice_Throws()
    {
        // Arrange
        var mockSession = Substitute.For<ITestSession>();
        var scope = new ServiceCollection().BuildServiceProvider().CreateAsyncScope();
        var mockContext = Substitute.For<IBrowserContext>();
        var scoped = new ScopedTestSession(mockSession, scope);
        scoped.SetContext(mockContext);

        // Act
        var act = () => scoped.SetContext(Substitute.For<IBrowserContext>());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been set*");
    }

    [Test]
    public void ScopedTestSession_SetContext_WithNull_Throws()
    {
        // Arrange
        var mockSession = Substitute.For<ITestSession>();
        var scope = new ServiceCollection().BuildServiceProvider().CreateAsyncScope();
        var scoped = new ScopedTestSession(mockSession, scope);

        // Act
        var act = () => scoped.SetContext(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ScopedTestSession Dispose Tests

    [Test]
    public async Task ScopedTestSession_DisposeAsync_WithoutContext_DoesNotThrow()
    {
        // Arrange
        var mockSession = Substitute.For<ITestSession>();
        var scope = new ServiceCollection().BuildServiceProvider().CreateAsyncScope();
        var scoped = new ScopedTestSession(mockSession, scope);

        // Act
        var act = async () => await scoped.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task ScopedTestSession_DisposeAsync_WithContext_ClosesContext()
    {
        // Arrange
        var mockSession = Substitute.For<ITestSession>();
        var scope = new ServiceCollection().BuildServiceProvider().CreateAsyncScope();
        var mockContext = Substitute.For<IBrowserContext>();
        var scoped = new ScopedTestSession(mockSession, scope);
        scoped.SetContext(mockContext);

        // Act
        await scoped.DisposeAsync();

        // Assert
        await mockContext.Received(1).CloseAsync();
    }

    #endregion

    #region TestSessionProvider Lifecycle Tests

    [Test]
    public void CreateTestSession_NoArgs_ReturnsSessionWithoutContext()
    {
        // Arrange
        var provider = new StubTestSessionProvider();

        // Act
        var scoped = provider.CreateTestSession();

        // Assert
        scoped.Should().NotBeNull();
        scoped.Session.Should().NotBeNull();
        scoped.Session.Should().BeOfType<StubTestSession>();
        scoped.HasContext.Should().BeFalse();
    }

    [Test]
    public void CreateTestSession_NoArgs_CalledTwice_CreatesFreshSession()
    {
        // Arrange
        var provider = new StubTestSessionProvider();

        // Act
        var scoped1 = provider.CreateTestSession();
        var scoped2 = provider.CreateTestSession();

        // Assert
        scoped1.Session.Should().NotBeSameAs(scoped2.Session);
    }

    [Test]
    public void CreateTestSession_NoArgs_CustomRegistrations_AreAvailable()
    {
        // Arrange
        var mockService = Substitute.For<IUnionService>();
        var provider = StubTestSessionProvider.CreateWithServices(services =>
        {
            services.AddSingleton<IEnumerable<IUnionService>>(new[] { mockService });
        });

        // Act
        var scoped = provider.CreateTestSession();

        // Assert
        scoped.Session.GetServices().Should().Contain(mockService);
    }

    #endregion

    #region Full Lifecycle Simulation (Session-first pattern)

    [Test]
    public void SessionFirstLifecycle_SessionAvailableBeforeContext()
    {
        // This simulates the UnionSetUp order:
        // 1. Create session (Session becomes available)
        // 2. Access Session (e.g., in ContextOptions)
        // 3. Create context
        // 4. Attach context (Context becomes available, services get context)

        // Arrange
        var provider = new StubTestSessionProvider();
        var mockContext = Substitute.For<IBrowserContext>();

        // Step 1: Create session without context
        var scopedSession = provider.CreateTestSession();

        // Step 2: Session IS accessible
        scopedSession.Session.Should().NotBeNull();

        // Context is NOT yet accessible
        var contextAccess = () => scopedSession.Context;
        contextAccess.Should().Throw<InvalidOperationException>();

        // Step 3 + 4: Attach context
        scopedSession.SetContext(mockContext);

        // Now Context IS accessible
        scopedSession.Context.Should().BeSameAs(mockContext);
    }

    #endregion
}
