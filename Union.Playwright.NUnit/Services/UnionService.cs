using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;
using Union.Playwright.NUnit.TestSession;

namespace Union.Playwright.NUnit.Services
{
    public abstract class UnionService<T> : IUnionService where T : IUnionPage
    {
        private readonly MatchUrlRouter _router;
        private readonly IServiceContextsPool _serviceContextsPool;
        private readonly TestSettings _testSettings;
        public IServiceContextsPool ServiceContextsPool => _serviceContextsPool;

        private IBrowserState? _state;
        public IBrowserState State => _state ??= new BrowserState(this);

        private IBrowserGo? _go;
        public IBrowserGo Go => _go ??= new BrowserGo(this, State, _serviceContextsPool, _testSettings);

        public UnionService(IServiceContextsPool serviceContextsPool, TestSettings? testSettings = null)
        {
            _serviceContextsPool = serviceContextsPool;
            _testSettings = testSettings ?? TestSettings.Default;
            _router = new MatchUrlRouter();
            _router.RegisterDerivedPages<T>();
        }

        public abstract string BaseUrl { get; }

        private Uri BaseUri => new Uri(BaseUrl);

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

        /// <summary>
        /// Async page resolution supporting MatchablePage with DOM checks.
        /// </summary>
        public ValueTask<IUnionPage?> GetPageAsync(RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
        {
            return _router.GetPageAsync(requestData, baseUrlInfo, playwrightPage);
        }

        /// <summary>
        /// Synchronous page resolution. Does not support MatchablePage.
        /// </summary>
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
}
