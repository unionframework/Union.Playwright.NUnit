using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Core
{
    public interface IBrowserAction
    {
        Task<TPage?> ClickAndWaitForRedirectAsync<TPage>(ILocator locator)
            where TPage : class, IUnionPage;

        Task<TModal?> ClickAndWaitForAlertAsync<TModal>(ILocator locator)
            where TModal : class, IUnionModal;

        Task<TComponent> ClickAndWaitForAsync<TComponent>(ILocator locator)
            where TComponent : ComponentBase;
    }
}
