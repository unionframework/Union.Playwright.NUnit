using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Services
{
    public interface IPageResolver
    {
        BaseUrlPattern BaseUrlPattern { get; }

        /// <summary>
        /// Async page resolution supporting MatchablePage with DOM checks.
        /// </summary>
        ValueTask<IUnionPage?> GetPageAsync(RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage);

        /// <summary>
        /// Synchronous page resolution. Does not support MatchablePage.
        /// </summary>
        [Obsolete("Use GetPageAsync instead. This method does not support MatchablePage.")]
        IUnionPage? GetPage(RequestData requestData, BaseUrlInfo baseUrlInfo);
    }
}
