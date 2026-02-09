using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Components
{
    public interface IContainer : IComponent
    {
        string RootScss { get; }

        string InnerScss(string relativeScss, params object[] args);
    }
}
