using Union.Playwright.NUnit.Core;
using System.Threading.Tasks;
using Union.Playwright.NUnit.Routing;
using System;
using Union.Playwright.NUnit.Services;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Pages.Interfaces;
using System.Diagnostics;

namespace Union.Playwright.NUnit.TestSession
{
    public class BrowserGo : IBrowserGo
    {
        private readonly IServiceContextsPool _serviceContextsPool;
        private readonly IUnionService _service;
        private readonly INavigationService _navigationService;
        private readonly IBrowserState _state;
        private readonly TestSettings _settings;

        public BrowserGo(IUnionService service, IBrowserState state, IServiceContextsPool serviceContextsPool, TestSettings? settings = null)
        {
            _service = service;
            _navigationService = service;
            _state = state;
            _serviceContextsPool = serviceContextsPool;
            _settings = settings ?? TestSettings.Default;
        }

        private async Task<IPage> GetPageAsync()
        {
            var context = await _serviceContextsPool.GetContext(_service).ConfigureAwait(false);
            if (context.Pages.Count == 0)
            {
                await context.NewPageAsync().ConfigureAwait(false);
            }
            // TODO: get active page instead of first
            return context.Pages[0];
        }

        public async virtual Task<T> ToPage<T>() where T : class, IUnionPage
        {
            var pageInstance = (T)Activator.CreateInstance(typeof(T));
            await this.ToPage(pageInstance);
            return _state.PageAs<T>()
                ?? throw new InvalidOperationException(
                    $"Navigation did not resolve to {typeof(T).Name}.\n" +
                    $"  Expected path: {pageInstance.AbsolutePath}\n" +
                    $"  Browser URL:   {_state.LastActualizedUrl}\n" +
                    $"  Base URL:      {_service.BaseUrl}\n" +
                    $"  Resolved as:   {(_state.Page != null ? _state.Page.GetType().Name : "(none)")}\n" +
                    $"  Diagnostics:   {_state.LastDiagnosticMessage}");
        }

        public async Task ToPage(IUnionPage page)
        {
            var requestData = _navigationService.GetRequestData(page);
            await this.ToUrl(requestData);
        }

        public async Task ToUrl(string url)
        {
            await this.ToUrl(new RequestData(url));
        }

        public async Task ToUrl(RequestData requestData)
        {
            var page = await this.GetPageAsync();
            await page.GotoAsync(requestData.Url.ToString());
            await this.AfterNavigateAsync(page);
        }

        public async Task Refresh()
        {
            var page = await this.GetPageAsync();
            await page.ReloadAsync();
            await this.AfterNavigateAsync(page);
        }

        public async Task Back()
        {
            var page = await this.GetPageAsync();
            await page.GoBackAsync();
            await this.AfterNavigateAsync(page);
        }

        private async Task AfterNavigateAsync(IPage page)
        {
            _state.Actualize(page);

            if (!_state.PageIs<IUnionPage>() && _settings.NavigationResolveTimeoutMs > 0)
            {
                var sw = Stopwatch.StartNew();
                while (!_state.PageIs<IUnionPage>() && sw.ElapsedMilliseconds < _settings.NavigationResolveTimeoutMs)
                {
                    await Task.Delay(_settings.NavigationPollIntervalMs);
                    _state.Actualize(page);
                }
            }

            if (_state.PageIs<IUnionPage>())
            {
                await _state.PageAs<IUnionPage>().WaitLoadedAsync();
            }
        }
    }
}
