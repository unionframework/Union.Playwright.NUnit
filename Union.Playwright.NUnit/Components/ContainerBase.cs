using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.SCSS;

namespace Union.Playwright.NUnit.Components
{
    public abstract class ContainerBase : ComponentBase, IContainer
    {
        protected ContainerBase(IUnionPage parentPage, string rootScss = null)
            : base(parentPage, rootScss) { }

        public string InnerScss(string relativeScss, params object[] args)
        {
            var formatted = string.Format(relativeScss, args);
            return ScssBuilder.Concat(this.RootScss, formatted).Value;
        }
    }
}
