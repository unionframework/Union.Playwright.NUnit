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
/// Provides test isolation for parallel execution via AsyncLocal storage.
/// Inherits from BrowserTest to get shared Browser instance.
/// </summary>
/// <typeparam name="TSession">The test session type containing services.</typeparam>
public abstract class UnionTest<TSession> : BrowserTest
    where TSession : class, ITestSession
{
    #region Properties

    /// <summary>
    /// Gets the test session for the current test.
    /// Contains all services configured in the TestSessionProvider.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if accessed before [SetUp] runs or after [TearDown] completes.
    /// </exception>
    protected TSession Session
    {
        get
        {
            var current = ScopedTestSession.Current;
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
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if accessed before [SetUp] runs.
    /// </exception>
    public IBrowserContext Context
    {
        get
        {
            var current = ScopedTestSession.Current;
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
    /// </summary>
    /// <returns>Options for the browser context.</returns>
    public virtual BrowserNewContextOptions ContextOptions() => new();

    #endregion

    #region SetUp / TearDown

    /// <summary>
    /// Creates an isolated browser context and test session for this test.
    /// Runs before each test method.
    /// </summary>
    [SetUp]
    public async Task UnionSetUp()
    {
        // Create isolated browser context for this test
        // This context is separate from any other parallel test's context
        var context = await Browser.NewContextAsync(ContextOptions());

        // Create the test session with DI scope
        var scopedSession = GetSessionProvider().CreateTestSession(context);

        // Store in AsyncLocal - this test's async execution flow will see this value
        // Other parallel tests have their own AsyncLocal value
        ScopedTestSession.SetCurrent(scopedSession);
    }

    /// <summary>
    /// Disposes the test session and browser context.
    /// Runs after each test method.
    /// Always clears AsyncLocal even if disposal fails.
    /// </summary>
    [TearDown]
    public async Task UnionTearDown()
    {
        var session = ScopedTestSession.Current;
        if (session != null)
        {
            try
            {
                // Dispose session (closes context, disposes DI scope)
                await session.DisposeAsync();
            }
            finally
            {
                // Always clear AsyncLocal, even if disposal failed
                ScopedTestSession.SetCurrent(null);
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
