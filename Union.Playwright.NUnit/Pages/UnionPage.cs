using Microsoft.Playwright;
using System.Collections.Generic;
using System.Threading.Tasks;
using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.Pages;

public abstract class UnionPage : IUnionPage
{
    public IPage PlaywrightPage { get; private set; }

    public IUnionService Service { get; private set; }

    public BaseUrlInfo BaseUrlInfo { get; set; }

    public List<Cookie> Cookies { get; set; }

    public Dictionary<string, string> Params { get; set; }

    public Dictionary<string, string> Data { get; set; }

    public abstract string AbsolutePath { get; }

    public List<IUnionModal> Modals { get; private set; }

    public List<ILoader> Loaders { get; private set; }

    public List<IOverlay> Overlays { get; private set; }

    public List<ComponentBase> Components { get; private set; }

    protected UnionPage()
    {
        this.Params = new Dictionary<string, string>();
        this.Data = new Dictionary<string, string>();
        this.Cookies = new List<Cookie>();
        this.Modals = new List<IUnionModal>();
        this.Loaders = new List<ILoader>();
        this.Overlays = new List<IOverlay>();
        this.Components = new List<ComponentBase>();
    }

    public void Activate(IPage page, IUnionService service)
    {
        this.PlaywrightPage = page;
        this.Service = service;
        WebPageBuilder.InitPage(this);
    }

    public virtual Task WaitLoadedAsync()
    {
        return Task.CompletedTask;
    }

    public void RegisterComponent(IComponent component)
    {
        if (component is ComponentBase cb)
            Components.Add(cb);

        if (component is IUnionModal modal)
            Modals.Add(modal);
        else if (component is ILoader loader)
            Loaders.Add(loader);
        else if (component is IOverlay overlay)
            Overlays.Add(overlay);
    }

    public T RegisterComponent<T>(string componentName, params object[] args) where T : IComponent
    {
        var component = CreateComponent<T>(args);
        RegisterComponent(component);
        component.ComponentName = componentName;
        return component;
    }

    public T CreateComponent<T>(params object[] args) where T : IComponent
    {
        return WebPageBuilder.CreateComponent<T>(this, args);
    }

    public RequestData GetRequest(BaseUrlInfo defaultBaseUrlInfo)
    {
        var url =
            new UriAssembler(BaseUrlInfo, AbsolutePath, Data, Params).Assemble(
                defaultBaseUrlInfo);
        return new RequestData(url);
    }
}
