using System;
using Microsoft.Playwright;
using System.Threading.Tasks;
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

        public ILocator RootLocator => this.PlaywrightPage.Locator(
            this.RootXcss.StartsWith("xpath=") ? this.RootXcss :
            this.RootXcss.StartsWith("/") ? "xpath=" + this.RootXcss :
            "xpath=" + Xcss.Parse(this.RootXcss).XPath);

        protected IBrowserGo Go => this.ParentPage.Service.Go;

        protected IBrowserState State => this.ParentPage.Service.State;

        protected IBrowserAction Action => this.ParentPage.Service.Action;

        public Task<bool> IsVisibleAsync() => this.RootLocator.IsVisibleAsync();
    }
}
