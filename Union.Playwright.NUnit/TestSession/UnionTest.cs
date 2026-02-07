using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.TestSession;

/// <summary>
/// Base class for all Union Playwright tests.
/// Provides test isolation for parallel execution via per-instance session storage.
/// Inherits from BrowserTest to get shared Browser instance.
/// </summary>
/// <typeparam name="TSession">The test session type containing services.</typeparam>
public abstract class UnionTest<TSession> : BrowserTest
    where TSession : class, ITestSession
{
    /// <summary>
    /// Per-instance session storage. Instance fields are used instead of AsyncLocal
    /// because async methods run in isolated ExecutionContexts that revert modifications.
    /// </summary>
    private ScopedTestSession? _scopedSession;

    protected TSession Session
    {
        get
        {
            var current = _scopedSession;
            if (current == null)
            {
                throw new InvalidOperationException(
                    "No test session available. " +
                    "Ensure you are accessing Session within a test method " +
                    "after [SetUp] has completed.");
            }
            return (TSession)current.Session;
        }
    }

    public IBrowserContext Context
    {
        get
        {
            var current = _scopedSession;
            if (current == null)
            {
                throw new InvalidOperationException(
                    "No test session available. " +
                    "Ensure you are accessing Context within a test method.");
            }
            return current.Context;
        }
    }

    protected abstract TestSessionProvider<TSession> GetSessionProvider();

    public virtual BrowserNewContextOptions ContextOptions() => new();

    /// <summary>
    /// Creates an isolated test session and browser context for this test.
    /// Runs before each test method.
    ///
    /// Lifecycle order:
    /// 1. DI scope created, session resolved (Session becomes available)
    /// 2. ContextOptions() called (can access Session for configuration)
    /// 3. Browser context created with the options
    /// 4. Context attached to session (Context becomes available)
    /// </summary>
    [SetUp]
    public async Task UnionSetUp()
    {
        // Step 1: Create the test session with DI scope (no browser context yet)
        var scopedSession = GetSessionProvider().CreateTestSession();

        // Step 2: Store as instance field - Session is now accessible via the property.
        _scopedSession = scopedSession;

        // Step 3: Call ContextOptions() - consumers can now access Session
        var options = ContextOptions();

        // Step 4: Create isolated browser context for this test
        var context = await Browser.NewContextAsync(options);

        // Step 5: Attach context to the scoped session
        scopedSession.SetContext(context);
    }

    [TearDown]
    public async Task UnionTearDown()
    {
        var session = _scopedSession;
        if (session != null)
        {
            try
            {
                // Dispose session (disposes DI scope, closes context if set)
                await session.DisposeAsync();
            }
            finally
            {
                // Always clear, even if disposal failed
                _scopedSession = null;
            }
        }
    }

    protected TService GetService<TService>() where TService : IUnionService
    {
        var service = Session.GetServices().OfType<TService>().FirstOrDefault();
        if (service == null)
        {
            throw new InvalidOperationException(
                $"Service of type {typeof(TService).Name} not found in test session. " +
                $"Ensure it is registered in the TestSessionProvider.");
        }
        return service;
    }
}
