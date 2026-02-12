using System;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.Components
{
    public abstract class ComponentBase : IComponent
    {
        private readonly string _rootScss;

        public IUnionPage ParentPage { get; }

        public string ComponentName { get; set; }

        public string FrameXcss { get; set; }

        protected ComponentBase(IUnionPage parentPage, string rootScss = null)
        {
            this.ParentPage = parentPage ?? throw new ArgumentNullException(nameof(parentPage));
            this._rootScss = rootScss;
        }

        public virtual string RootScss => this._rootScss ?? "html";

        protected IPage PlaywrightPage => this.ParentPage.PlaywrightPage;

        public ILocator RootLocator => this.PlaywrightPage.Locator(this.RootScss);

        protected IBrowserGo Go => this.ParentPage.Service.Go;

        protected IBrowserState State => this.ParentPage.Service.State;

        protected IBrowserAction Action => this.ParentPage.Service.Action;

        public Task<bool> IsVisibleAsync() => this.RootLocator.IsVisibleAsync();
    }
}
