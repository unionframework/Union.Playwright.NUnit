using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Core;

namespace Union.Playwright.NUnit.TestSession;

/// <summary>
/// Holds all state for a single test execution.
/// Uses AsyncLocal for thread-safe isolation in parallel execution.
/// </summary>
public sealed class ScopedTestSession : IAsyncDisposable
{
    #region Static AsyncLocal Storage

    private static readonly AsyncLocal<ScopedTestSession?> _current = new();

    /// <summary>
    /// Gets the current test's session from AsyncLocal storage.
    /// Returns null if no test is currently executing in this async context.
    /// </summary>
    public static ScopedTestSession? Current => _current.Value;

    /// <summary>
    /// Sets the current test's session in AsyncLocal storage.
    /// Call with null to clear after test completion.
    /// </summary>
    public static void SetCurrent(ScopedTestSession? session) => _current.Value = session;

    #endregion

    #region Instance Properties

    /// <summary>
    /// The test session containing all services and configuration.
    /// </summary>
    public ITestSession Session { get; }

    /// <summary>
    /// The DI scope for this test. Disposed when test completes.
    /// </summary>
    public AsyncServiceScope Scope { get; }

    /// <summary>
    /// The browser context for this test.
    /// All services share this context (same cookies, storage, auth).
    /// Each service creates its own Page within this context.
    /// </summary>
    public IBrowserContext Context { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new scoped test session.
    /// </summary>
    public ScopedTestSession(
        ITestSession session,
        AsyncServiceScope scope,
        IBrowserContext context)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        Scope = scope;
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Disposes the DI scope and closes the browser context.
    /// Closing the context automatically closes all pages within it.
    /// Both resources are always attempted to be disposed, even if one fails.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        Exception? scopeException = null;
        Exception? contextException = null;

        // Always attempt to dispose the DI scope
        try
        {
            await this.Scope.DisposeAsync();
        }
        catch (Exception ex)
        {
            scopeException = ex;
        }

        // Always attempt to close the browser context
        try
        {
            await this.Context.CloseAsync();
        }
        catch (Exception ex)
        {
            contextException = ex;
        }

        // If both failed, throw aggregate exception
        if (scopeException != null && contextException != null)
        {
            throw new AggregateException(
                "Multiple errors during test session disposal",
                scopeException, contextException);
        }

        // If only one failed, throw that one
        if (scopeException != null)
            throw scopeException;

        if (contextException != null)
            throw contextException;
    }

    #endregion
}
