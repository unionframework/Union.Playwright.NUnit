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
    #region Fields

    /// <summary>
    /// Instance field storing the scoped session for the current test.
    /// Used instead of AsyncLocal because async [SetUp] methods run in
    /// an isolated ExecutionContext — AsyncLocal modifications inside
    /// async methods are reverted when the method returns.
    /// Instance fields persist across [SetUp] calls on the same test instance.
    /// </summary>
    private ScopedTestSession? _scopedSession;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the test session for the current test.
    /// Contains all services configured in the TestSessionProvider.
    /// Available during [SetUp], test methods, and [TearDown].
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if accessed before [SetUp] runs or after [TearDown] completes.
    /// </exception>
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

    /// <summary>
    /// Gets the browser context for the current test.
    /// All services in this test share this context (cookies, storage, auth).
    /// Available after browser context creation in [SetUp] completes.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if accessed before [SetUp] runs.
    /// </exception>
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

    #endregion

    #region Abstract Members

    /// <summary>
    /// Implement to provide the TestSessionProvider that configures services.
    /// Typically returns a static singleton instance.
    /// </summary>
    protected abstract TestSessionProvider<TSession> GetSessionProvider();

    #endregion

    #region Virtual Members

    /// <summary>
    /// Override to customize browser context options for tests in this class.
    /// Called during [SetUp] when creating the context.
    /// Session is available when this method is called.
    /// </summary>
    /// <returns>Options for the browser context.</returns>
    public virtual BrowserNewContextOptions ContextOptions() => new();

    #endregion

    #region SetUp / TearDown

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
        // NOTE: We use an instance field instead of AsyncLocal because
        // AsyncLocal modifications inside async methods are reverted when the
        // method returns (ExecutionContext copy-on-write semantics).
        // Instance fields persist across [SetUp] calls on the same test instance.
        _scopedSession = scopedSession;

        // Step 3: Call ContextOptions() - consumers can now access Session
        var options = ContextOptions();

        // Step 4: Create isolated browser context for this test
        var context = await Browser.NewContextAsync(options);

        // Step 5: Attach context to the scoped session
        scopedSession.SetContext(context);
    }

    /// <summary>
    /// Disposes the test session and browser context.
    /// Runs after each test method.
    /// Always clears the instance field even if disposal fails.
    /// </summary>
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

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets a service of the specified type from the test session.
    /// </summary>
    /// <typeparam name="TService">The service type to retrieve.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the service is not found.
    /// </exception>
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

    #endregion
}
