using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Routing
{
    public interface IRouter
    {
        RequestData GetRequest(IUnionPage page, BaseUrlInfo defaultBaseUrlInfo);

        /// <summary>
        /// Async page resolution supporting MatchablePage with DOM checks.
        /// Checks MatchablePage instances FIRST, then falls back to regular UnionPage.
        /// </summary>
        ValueTask<IUnionPage?> GetPageAsync(RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage);

        /// <summary>
        /// Synchronous page resolution. Does not support MatchablePage.
        /// </summary>
        [Obsolete("Use GetPageAsync instead. This method does not support MatchablePage.")]
        IUnionPage? GetPage(RequestData requestData, BaseUrlInfo baseUrlInfo);

        IReadOnlyList<Type> GetPageTypes();

        bool HasPage(IUnionPage page);
    }
}