using Microsoft.Playwright;

namespace Union.Playwright.NUnit.Services;

/// <summary>
/// Internal interface for services that need browser context injection.
/// Implemented by UnionService via explicit interface implementation
/// so it doesn't pollute the public API.
/// </summary>
internal interface IBrowserContextAware
{
    /// <summary>
    /// Sets the browser context for this service.
    /// Called by ScopedTestSession.SetContext() during test setup.
    /// </summary>
    void SetBrowserContext(IBrowserContext context);
}
