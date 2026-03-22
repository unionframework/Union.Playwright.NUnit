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

    public BrowserGo(IUnionService service, IBrowserState state, TestSettings? settings = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _navigationService = service;
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _settings = settings ?? TestSettings.Default;
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
        var page = await _service.GetOrCreatePageAsync();
        await page.GotoAsync(requestData.Url.ToString());
        await AfterNavigateAsync(page);
    }

    /// <summary>
    /// Refreshes the current page.
    /// </summary>
    public async Task Refresh()
    {
        var page = await _service.GetOrCreatePageAsync();
        await page.ReloadAsync();
        await AfterNavigateAsync(page);
    }

    /// <summary>
    /// Navigates back in browser history.
    /// </summary>
    public async Task Back()
    {
        var page = await _service.GetOrCreatePageAsync();
        await page.GoBackAsync();
        await AfterNavigateAsync(page);
    }

    private async Task AfterNavigateAsync(IPage page)
    {
        try
        {
            await _state.ActualizeAsync(page);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to actualize page state after navigation to '{page.Url}'", ex);
        }

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
                }
            }

            if (!_state.PageIs<IUnionPage>())
            {
                _state.AppendDiagnosticMessage(
                    $"Page resolution timed out after {attemptCount} attempts ({sw.ElapsedMilliseconds}ms).");
            }
        }

        if (_state.PageIs<IUnionPage>())
        {
            var resolvedPage = _state.PageAs<IUnionPage>()!;

            var waitLoadedTimeoutMs = _settings.WaitLoadedTimeoutMs;

            if (waitLoadedTimeoutMs > 0)
            {
                var waitTask = resolvedPage.WaitLoadedAsync();
                var timeoutTask = Task.Delay(waitLoadedTimeoutMs);

                var completedTask = await Task.WhenAny(waitTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException(
                        $"Page '{resolvedPage.GetType().Name}' WaitLoadedAsync " +
                        $"timed out after {waitLoadedTimeoutMs}ms");
                }

                await waitTask;
            }
            else
            {
                await resolvedPage.WaitLoadedAsync();
            }
        }
    }
}
