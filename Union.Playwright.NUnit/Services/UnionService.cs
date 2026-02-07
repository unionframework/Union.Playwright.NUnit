using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;
using Union.Playwright.NUnit.TestSession;

namespace Union.Playwright.NUnit.Services;

/// <summary>
/// Base class for services that interact with web pages.
/// Each service instance has its own page (tab) within the shared browser context.
/// </summary>
/// <typeparam name="T">The base page type for this service.</typeparam>
public abstract class UnionService<T> : IUnionService, IBrowserContextAware where T : IUnionPage
{
    private readonly MatchUrlRouter _router;
    private readonly TestSettings _testSettings;
    private IPage? _page;
    private IBrowserContext? _browserContext;

    private IBrowserState? _state;
    public IBrowserState State => _state ??= new BrowserState(this);

    private IBrowserGo? _go;
    public IBrowserGo Go => _go ??= new BrowserGo(this, State, _testSettings);

    private IBrowserAction? _action;
    public IBrowserAction Action => _action ??= new BrowserAction(this, State);

    public UnionService(TestSettings? testSettings = null)
    {
        _testSettings = testSettings ?? TestSettings.Default;
        _router = new MatchUrlRouter();
        _router.RegisterDerivedPages<T>();
    }

    public abstract string BaseUrl { get; }

    private Uri? _baseUri;
    private Uri BaseUri => _baseUri ??= new Uri(BaseUrl);

    public string AbsolutePath => BaseUri.AbsolutePath == "/" ? "" : BaseUri.AbsolutePath;

    public string Host => BaseUri.Authority;

    private BaseUrlPattern? _baseUrlPattern;
    public BaseUrlPattern BaseUrlPattern => _baseUrlPattern ??= BuildBaseUrlPattern();

    private BaseUrlPattern BuildBaseUrlPattern()
    {
        var urlRegexBuilder = new BaseUrlRegexBuilder(Host);
        if (!string.IsNullOrWhiteSpace(AbsolutePath))
        {
            urlRegexBuilder.SetAbsolutePathPattern(AbsolutePath.Replace("/", "\\/"));
        }
        return new BaseUrlPattern(urlRegexBuilder.Build());
    }

    private BaseUrlInfo DefaultBaseUrlInfo => new BaseUrlInfo(Host, AbsolutePath);

    void IBrowserContextAware.SetBrowserContext(IBrowserContext context)
    {
        _browserContext = context;
    }

    /// <summary>
    /// Gets or creates the page for this service.
    /// Each service has its own tab within the shared browser context.
    /// The page is created lazily on first access.
    /// </summary>
    /// <returns>The page for this service.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if called outside of a test context.
    /// </exception>
    public async Task<IPage> GetOrCreatePageAsync()
    {
        if (_page == null)
        {
            if (_browserContext == null)
            {
                throw new InvalidOperationException(
                    "Cannot create page: no browser context available. " +
                    "Ensure this is called within a test method after [SetUp].");
            }

            try
            {
                _page = await _browserContext.NewPageAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to create new page in browser context for service '{this.GetType().Name}'. " +
                    $"Browser context may be closed or browser may have crashed.", ex);
            }
        }

        return _page;
    }

    public ValueTask<IUnionPage?> GetPageAsync(RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
    {
        return _router.GetPageAsync(requestData, baseUrlInfo, playwrightPage);
    }

    [Obsolete("Use GetPageAsync instead. This method does not support MatchablePage.")]
    public IUnionPage? GetPage(RequestData requestData, BaseUrlInfo baseUrlInfo)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return _router.GetPage(requestData, baseUrlInfo);
#pragma warning restore CS0618
    }

    public RequestData GetRequestData(IUnionPage page)
    {
        return _router.GetRequest(page, DefaultBaseUrlInfo);
    }

    public bool HasPage(IUnionPage page)
    {
        return _router.HasPage(page);
    }
}
