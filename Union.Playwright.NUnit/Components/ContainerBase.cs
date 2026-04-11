using Union.Playwright.NUnit.Pages.Interfaces;
using XcssSelectors;

namespace Union.Playwright.NUnit.Components
{
    public abstract class ContainerBase : ComponentBase, IContainer
    {
        protected ContainerBase(IUnionPage parentPage, string rootXcss = null)
            : base(parentPage, rootXcss) { }

        public Xcss InnerXcss(string relativeXcss, params object[] args)
        {
            var formatted = string.Format(relativeXcss, args);
            return XcssFromSelector(this.RootXcss).Concat(XcssFromSelector(formatted));
        }

        private static Xcss XcssFromSelector(string selector)
        {
            if (selector.StartsWith("xpath="))
                return Xcss.FromXPath(selector.Substring("xpath=".Length));
            if (selector.StartsWith("/"))
                return Xcss.FromXPath(selector);
            return Xcss.Parse(selector);
        }
    }
}
