using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.TestSession
{
    internal class BrowserState : IBrowserState
    {
        private readonly IPageResolver _pageResolver;
        public IModalWindow? ModalWindow { get; private set; }
        public IUnionPage? Page { get; private set; }
        public string? LastActualizedUrl { get; private set; }
        public string? LastDiagnosticMessage { get; private set; }

        public BrowserState(IPageResolver pageResolver)
        {
            _pageResolver = pageResolver;
        }

        /// <summary>
        /// Async actualization supporting MatchablePage with DOM checks.
        /// </summary>
        public async ValueTask ActualizeAsync(IPage page)
        {
            this.Page = null;
            LastActualizedUrl = page.Url;
            var baseUrlPattern = _pageResolver.BaseUrlPattern;
            var result = baseUrlPattern.Match(page.Url);

            if (result.Level == BaseUrlMatchLevel.FullDomain)
            {
                this.Page = await _pageResolver.GetPageAsync(
                    new Routing.RequestData(page.Url),
                    result.GetBaseUrlInfo(),
                    page);

                if (this.Page != null)
                {
                    this.Page.Activate(page);
                    LastDiagnosticMessage = $"Resolved to {this.Page.GetType().Name}";
                }
                else
                {
                    LastDiagnosticMessage = $"Base URL matched (FullDomain) but no page pattern matched for path in '{page.Url}'";
                }
            }
            else
            {
                LastDiagnosticMessage = $"Base URL did not match (level: {result.Level}). URL: '{page.Url}', Pattern: '{baseUrlPattern}'";
            }
        }

        /// <summary>
        /// Synchronous actualization. Does not support MatchablePage.
        /// </summary>
        [Obsolete("Use ActualizeAsync instead. This method does not support MatchablePage.")]
        public void Actualize(IPage page)
        {
            this.Page = null;
            LastActualizedUrl = page.Url;
            var baseUrlPattern = _pageResolver.BaseUrlPattern;
            var result = baseUrlPattern.Match(page.Url);
            if (result.Level == BaseUrlMatchLevel.FullDomain)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                this.Page = _pageResolver.GetPage(new Routing.RequestData(page.Url), result.GetBaseUrlInfo());
#pragma warning restore CS0618
                if (this.Page != null)
                {
                    this.Page.Activate(page);
                    LastDiagnosticMessage = $"Resolved to {this.Page.GetType().Name}";
                }
                else
                {
                    LastDiagnosticMessage = $"Base URL matched (FullDomain) but no page pattern matched for path in '{page.Url}'";
                }
            }
            else
            {
                LastDiagnosticMessage = $"Base URL did not match (level: {result.Level}). URL: '{page.Url}', Pattern: '{baseUrlPattern}'";
            }
        }

        public T? PageAs<T>() where T : class, IUnionPage => this.Page as T;

        public bool PageIs<T>() where T : IUnionPage
        {
            if (this.Page == null)
            {
                return false;
            }

            return this.Page is T;
        }
    }
}
