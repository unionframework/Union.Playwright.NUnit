using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NSubstitute;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.Tests.Fakes;

/// <summary>
/// A fake IUnionService implementation for testing.
/// </summary>
public class FakeUnionService : IUnionService, IBrowserContextAware
{
    private readonly string _baseUrl;
    private IPage? _page;
    private IBrowserContext? _browserContext;

    public FakeUnionService(string baseUrl = "https://test.example.com")
    {
        _baseUrl = baseUrl;
        State = Substitute.For<IBrowserState>();
        Go = Substitute.For<IBrowserGo>();
    }

    public string BaseUrl => _baseUrl;

    public BaseUrlPattern BaseUrlPattern => new BaseUrlPattern(new BaseUrlRegexBuilder(_baseUrl).Build());

    public IBrowserState State { get; }

    public IBrowserGo Go { get; }

    /// <summary>
    /// Sets the browser context for this service.
    /// Called by ScopedTestSession.SetContext() during test setup.
    /// </summary>
    void IBrowserContextAware.SetBrowserContext(IBrowserContext context)
    {
        _browserContext = context;
    }

    /// <summary>
    /// Gets or creates a page for this service.
    /// Uses injected browser context, or falls back to a mock for unit tests.
    /// </summary>
    public async Task<IPage> GetOrCreatePageAsync()
    {
        if (_page == null)
        {
            if (_browserContext != null)
            {
                _page = await _browserContext.NewPageAsync();
            }
            else
            {
                // For unit tests without a real session, return a mock
                _page = Substitute.For<IPage>();
            }
        }
        return _page;
    }

    public ValueTask<IUnionPage?> GetPageAsync(RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
    {
        return ValueTask.FromResult<IUnionPage?>(Substitute.For<IUnionPage>());
    }

    [Obsolete("Use GetPageAsync instead")]
    public IUnionPage? GetPage(RequestData requestData, BaseUrlInfo baseUrlInfo)
    {
        return Substitute.For<IUnionPage>();
    }

    public RequestData GetRequestData(IUnionPage page)
    {
        return new RequestData(_baseUrl);
    }

    public bool HasPage(IUnionPage page)
    {
        return true;
    }
}
