using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.TestSession;

/// <summary>
/// Holds all state for a single test execution.
/// Each test gets its own instance via UnionTest's per-instance field.
/// </summary>
public sealed class ScopedTestSession : IAsyncDisposable
{
    #region Instance Properties

    /// <summary>
    /// The test session containing all services and configuration.
    /// </summary>
    public ITestSession Session { get; }

    /// <summary>
    /// The DI scope for this test. Disposed when test completes.
    /// </summary>
    public AsyncServiceScope Scope { get; }

    private IBrowserContext? _context;

    /// <summary>
    /// The browser context for this test.
    /// All services share this context (same cookies, storage, auth).
    /// Each service creates its own Page within this context.
    /// Available after SetContext() is called during [SetUp].
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if accessed before the browser context has been set via SetContext().
    /// </exception>
    public IBrowserContext Context =>
        _context ?? throw new InvalidOperationException(
            "Browser context is not yet available.");

    /// <summary>
    /// Gets whether the browser context has been set.
    /// </summary>
    internal bool HasContext => _context != null;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new scoped test session without a browser context.
    /// Call SetContext() to attach the browser context after creation.
    /// </summary>
    public ScopedTestSession(
        ITestSession session,
        AsyncServiceScope scope)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        Scope = scope;
    }

    #endregion

    #region Context Binding

    /// <summary>
    /// Attaches the browser context to this session and pushes it to all services.
    /// Called by UnionTest.UnionSetUp() after ContextOptions() resolves.
    /// </summary>
    /// <param name="context">The browser context for this test.</param>
    /// <exception cref="ArgumentNullException">Thrown if context is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if context has already been set.</exception>
    internal void SetContext(IBrowserContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (_context != null)
            throw new InvalidOperationException(
                "Browser context has already been set for this session.");
        _context = context;

        // Push context to all services that need it for lazy page creation
        var services = Session.GetServices();
        if (services != null)
        {
            foreach (var service in services)
            {
                if (service is IBrowserContextAware aware)
                {
                    aware.SetBrowserContext(context);
                }
            }
        }
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Disposes the DI scope and closes the browser context (if set).
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

        // Only attempt to close browser context if it was set
        if (_context != null)
        {
            try
            {
                await _context.CloseAsync();
            }
            catch (Exception ex)
            {
                contextException = ex;
            }
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
