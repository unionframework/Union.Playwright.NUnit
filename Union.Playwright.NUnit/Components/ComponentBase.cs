using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Services;
using XcssSelectors;

namespace Union.Playwright.NUnit.Components
{
    public abstract class ComponentBase : IComponent
    {
        private readonly string _rootXcss;

        public IUnionPage ParentPage { get; }

        public string ComponentName { get; set; }

        public string FrameXcss { get; set; }

        protected ComponentBase(IUnionPage parentPage, string rootXcss = null)
        {
            this.ParentPage = parentPage ?? throw new ArgumentNullException(nameof(parentPage));
            this._rootXcss = rootXcss;
        }

        public virtual string RootXcss => this._rootXcss ?? "html";

        protected IPage PlaywrightPage => this.ParentPage.PlaywrightPage;

        public ILocator RootLocator => this.GetLocatorFor(this.RootXcss);

        // Resolves an absolute selector to a Playwright locator: an "xpath="-prefixed or raw-XPath
        // selector is taken as-is; any other selector is parsed as XCSS. The selector is never scoped
        // against a parent here — callers that need scoping build it in (via InnerXcss).
        protected ILocator GetLocatorFor(string selector) =>
            this.PlaywrightPage.Locator(
                selector.StartsWith("xpath=") ? selector
                : selector.StartsWith("/") ? "xpath=" + selector
                : "xpath=" + Xcss.Parse(selector).XPath
            );

        protected IBrowserGo Go => this.ParentPage.Service.Go;

        protected IBrowserState State => this.ParentPage.Service.State;

        protected IBrowserAction Action => this.ParentPage.Service.Action;

        public Task<bool> IsVisibleAsync() => this.RootLocator.IsVisibleAsync();

        protected static ILocatorAssertions Expect(ComponentBase element) =>
            Assertions.Expect(element.RootLocator);
    }
}
