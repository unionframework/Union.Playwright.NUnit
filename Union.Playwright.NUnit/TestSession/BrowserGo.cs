using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.TestSession;

/// <summary>
/// Provides navigation capabilities for a service.
/// Uses the owning service's page for all navigation.
/// </summary>
public class BrowserGo : IBrowserGo
{
    private readonly IUnionService _service;
    private readonly INavigationService _navigationService;
    private readonly IBrowserState _state;
    private readonly TestSettings _settings;

    /// <summary>
    /// Initializes a new BrowserGo instance.
    /// </summary>
    /// <param name="service">The service that owns this navigation helper.</param>
    /// <param name="state">The browser state tracker.</param>
    /// <param name="settings">Optional test settings.</param>
    public BrowserGo(IUnionService service, IBrowserState state, TestSettings? settings = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _navigationService = service;
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _settings = settings ?? TestSettings.Default;
    }

    /// <summary>
    /// Gets the page for the owning service.
    /// Delegates to the service's lazy page creation.
    /// </summary>
    private async Task<IPage> GetPageAsync()
    {
        return await _service.GetOrCreatePageAsync();
    }

    /// <summary>
    /// Navigates to a page of the specified type.
    /// </summary>
    /// <typeparam name="T">The page type to navigate to.</typeparam>
    /// <returns>The resolved page instance.</returns>
    public virtual async Task<T> ToPage<T>() where T : class, IUnionPage
    {
        var pageInstance = (T)Activator.CreateInstance(typeof(T))!;
        await ToPage(pageInstance);
        return _state.PageAs<T>()
            ?? throw new InvalidOperationException(
                $"Navigation did not resolve to {typeof(T).Name}.\n" +
                $"  Expected path: {pageInstance.AbsolutePath}\n" +
                $"  Browser URL:   {_state.LastActualizedUrl}\n" +
                $"  Base URL:      {_service.BaseUrl}\n" +
                $"  Resolved as:   {(_state.Page != null ? _state.Page.GetType().Name : "(none)")}\n" +
                $"  Diagnostics:   {_state.LastDiagnosticMessage}");
    }

    /// <summary>
    /// Navigates to the specified page instance.
    /// </summary>
    public async Task ToPage(IUnionPage page)
    {
        var requestData = _navigationService.GetRequestData(page);
        await ToUrl(requestData);
    }

    /// <summary>
    /// Navigates to the specified URL.
    /// </summary>
    public async Task ToUrl(string url)
    {
        await ToUrl(new RequestData(url));
    }

    /// <summary>
    /// Navigates to the specified request data.
    /// </summary>
    public async Task ToUrl(RequestData requestData)
    {
        var page = await GetPageAsync();
        await page.GotoAsync(requestData.Url.ToString());
        await AfterNavigateAsync(page);
    }

    /// <summary>
    /// Refreshes the current page.
    /// </summary>
    public async Task Refresh()
    {
        var page = await GetPageAsync();
        await page.ReloadAsync();
        await AfterNavigateAsync(page);
    }

    /// <summary>
    /// Navigates back in browser history.
    /// </summary>
    public async Task Back()
    {
        var page = await GetPageAsync();
        await page.GoBackAsync();
        await AfterNavigateAsync(page);
    }

    /// <summary>
    /// Handles post-navigation tasks like waiting for page to load and actualizing state.
    /// </summary>
    private async Task AfterNavigateAsync(IPage page)
    {
        // Initial actualization attempt with error context
        try
        {
            await _state.ActualizeAsync(page);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to actualize page state after navigation to '{page.Url}'", ex);
        }

        // Retry loop if page not resolved and timeout configured
        if (!_state.PageIs<IUnionPage>() && _settings.NavigationResolveTimeoutMs > 0)
        {
            var sw = Stopwatch.StartNew();
            var attemptCount = 0;

            while (!_state.PageIs<IUnionPage>() &&
                   sw.ElapsedMilliseconds < _settings.NavigationResolveTimeoutMs)
            {
                attemptCount++;
                await Task.Delay(_settings.NavigationPollIntervalMs);

                try
                {
                    await _state.ActualizeAsync(page);
                }
                catch
                {
                    // Swallow exceptions during retry - we'll try again
                }
            }

            // Update diagnostic message if we timed out
            if (!_state.PageIs<IUnionPage>())
            {
                _state.AppendDiagnosticMessage(
                    $"Page resolution timed out after {attemptCount} attempts ({sw.ElapsedMilliseconds}ms).");
            }
        }

        // Call WaitLoadedAsync with timeout protection
        if (this._state.PageIs<IUnionPage>())
        {
            var resolvedPage = this._state.PageAs<IUnionPage>()!;

            var waitLoadedTimeoutMs = this._settings.WaitLoadedTimeoutMs;

            if (waitLoadedTimeoutMs > 0)
            {
                // Apply timeout protection
                var waitTask = resolvedPage.WaitLoadedAsync();
                var timeoutTask = Task.Delay(waitLoadedTimeoutMs);

                var completedTask = await Task.WhenAny(waitTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Timeout occurred
                    throw new TimeoutException(
                        $"Page '{resolvedPage.GetType().Name}' WaitLoadedAsync " +
                        $"timed out after {waitLoadedTimeoutMs}ms");
                }

                // WaitLoadedAsync completed - propagate any exception
                await waitTask;
            }
            else
            {
                // No timeout configured - await directly
                await resolvedPage.WaitLoadedAsync();
            }
        }
    }
}
