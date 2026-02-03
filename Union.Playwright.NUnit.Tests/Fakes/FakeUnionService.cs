using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NSubstitute;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;
using Union.Playwright.NUnit.Services;
using Union.Playwright.NUnit.TestSession;

namespace Union.Playwright.NUnit.Tests.Fakes;

/// <summary>
/// A fake IUnionService implementation for testing.
/// </summary>
public class FakeUnionService : IUnionService
{
    private readonly string _baseUrl;
    private IPage? _page;

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
    /// Gets or creates a page for this service.
    /// For tests, returns a mock page or uses the ScopedTestSession if available.
    /// </summary>
    public async Task<IPage> GetOrCreatePageAsync()
    {
        if (_page == null)
        {
            var currentSession = ScopedTestSession.Current;
            if (currentSession != null)
            {
                _page = await currentSession.Context.NewPageAsync();
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
