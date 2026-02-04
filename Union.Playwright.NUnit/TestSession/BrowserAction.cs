using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.TestSession;

/// <summary>
/// Provides click-and-wait actions that actualize browser state after interaction.
/// Uses Playwright's native waiting instead of polling.
/// </summary>
public class BrowserAction : IBrowserAction
{
    private readonly IUnionService _service;
    private readonly IBrowserState _state;

    public BrowserAction(IUnionService service, IBrowserState state)
    {
        this._service = service ?? throw new ArgumentNullException(nameof(service));
        this._state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <summary>
    /// Clicks the locator, waits for a URL change, actualizes state,
    /// and returns the resolved page as <typeparamref name="TPage"/>.
    /// Returns null if no redirect occurred or the page did not resolve to the expected type.
    /// </summary>
    public async Task<TPage?> ClickAndWaitForRedirectAsync<TPage>(ILocator locator)
        where TPage : class, IUnionPage
    {
        var page = await this._service.GetOrCreatePageAsync();
        var oldUrl = page.Url;

        await locator.ClickAsync();

        try
        {
            await page.WaitForURLAsync(url => url != oldUrl);
        }
        catch (TimeoutException)
        {
            return null;
        }

        await this._state.ActualizeAsync(page);
        return this._state.PageAs<TPage>();
    }

    /// <summary>
    /// Clicks the locator, waits for a modal of type <typeparamref name="TModal"/> to become visible,
    /// actualizes state, and returns the modal.
    /// Returns null if the modal type is not registered on the current page or did not appear.
    /// </summary>
    public async Task<TModal?> ClickAndWaitForAlertAsync<TModal>(ILocator locator)
        where TModal : class, IUnionModal
    {
        var page = await this._service.GetOrCreatePageAsync();

        var currentPage = this._state.Page;
        if (currentPage == null) return null;

        var modal = currentPage.Modals.OfType<TModal>().FirstOrDefault();
        if (modal == null) return null;

        await locator.ClickAsync();

        if (modal is ComponentBase component)
        {
            try
            {
                await page.Locator(component.RootScss)
                    .WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible
                    });
            }
            catch (TimeoutException)
            {
                return null;
            }
        }

        await this._state.ActualizeAsync(page);
        return this._state.ModalWindow as TModal;
    }
}
