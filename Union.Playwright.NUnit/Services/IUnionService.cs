using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Services;

/// <summary>
/// Defines the contract for a service that interacts with web pages.
/// Each service represents an application under test with its own base URL and pages.
/// </summary>
public interface IUnionService : IPageResolver, INavigationService
{
    /// <summary>
    /// Gets the base URL for this service.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Checks if the specified page belongs to this service.
    /// </summary>
    bool HasPage(IUnionPage page);

    /// <summary>
    /// Gets the current browser state for this service.
    /// </summary>
    IBrowserState State { get; }

    /// <summary>
    /// Gets the navigation helper for this service.
    /// </summary>
    IBrowserGo Go { get; }

    /// <summary>
    /// Gets the action helper for this service.
    /// </summary>
    IBrowserAction Action { get; }

    /// <summary>
    /// Gets the existing page for this service, or creates a new one if none exists.
    /// Each service has its own page (tab) within the shared browser context.
    /// </summary>
    /// <returns>The page for this service.</returns>
    Task<IPage> GetOrCreatePageAsync();
}
