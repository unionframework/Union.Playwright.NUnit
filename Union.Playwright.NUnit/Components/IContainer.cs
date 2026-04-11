using Union.Playwright.NUnit.Pages.Interfaces;
using XcssSelectors;

namespace Union.Playwright.NUnit.Components
{
    public interface IContainer : IComponent
    {
        string RootXcss { get; }

        Xcss InnerXcss(string relativeXcss, params object[] args);
    }
}
