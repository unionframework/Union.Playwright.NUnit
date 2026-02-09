using Microsoft.Playwright;
using System.Collections.Generic;
using System.Threading.Tasks;
using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Routing;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.Pages.Interfaces
{
    public interface IUnionPage
    {
        string AbsolutePath { get; }

        IPage PlaywrightPage { get; }

        IUnionService Service { get; }

        List<Cookie> Cookies { get; set; }

        Dictionary<string, string> Data { get; set; }

        Dictionary<string, string> Params { get; set; }

        BaseUrlInfo BaseUrlInfo { get; set; }

        List<IUnionModal> Modals { get; }

        List<ILoader> Loaders { get; }

        List<IOverlay> Overlays { get; }

        List<ComponentBase> Components { get; }

        void RegisterComponent(IComponent component);

        Task WaitLoadedAsync();

        RequestData GetRequest(BaseUrlInfo defaultBaseUrlInfo);

        void Activate(IPage page, IUnionService service);
    }
}
