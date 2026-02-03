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

        public void AppendDiagnosticMessage(string additionalInfo)
        {
            if (string.IsNullOrEmpty(this.LastDiagnosticMessage))
            {
                this.LastDiagnosticMessage = additionalInfo;
            }
            else
            {
                this.LastDiagnosticMessage = $"{additionalInfo} {this.LastDiagnosticMessage}";
            }
        }

        public BrowserState(IPageResolver pageResolver)
        {
            _pageResolver = pageResolver;
        }

        /// <summary>
        /// Async actualization supporting MatchablePage with DOM checks.
        /// </summary>
        public async ValueTask ActualizeAsync(IPage page)
        {
            await ActualizeInternalAsync(page, useAsync: true);
        }

        /// <summary>
        /// Synchronous actualization. Does not support MatchablePage.
        /// </summary>
        [Obsolete("Use ActualizeAsync instead. This method does not support MatchablePage.")]
        public void Actualize(IPage page)
        {
            ActualizeInternalAsync(page, useAsync: false).AsTask().GetAwaiter().GetResult();
        }

        private async ValueTask ActualizeInternalAsync(IPage page, bool useAsync)
        {
            this.Page = null;
            this.LastActualizedUrl = page.Url;

            var baseUrlPattern = this._pageResolver.BaseUrlPattern;
            var result = baseUrlPattern.Match(page.Url);

            if (result.Level == BaseUrlMatchLevel.FullDomain)
            {
                IUnionPage? resolvedPage;

                if (useAsync)
                {
                    resolvedPage = await this._pageResolver.GetPageAsync(
                        new Routing.RequestData(page.Url),
                        result.GetBaseUrlInfo(),
                        page);
                }
                else
                {
#pragma warning disable CS0618 // Intentional use of obsolete method
                    resolvedPage = this._pageResolver.GetPage(
                        new Routing.RequestData(page.Url),
                        result.GetBaseUrlInfo());
#pragma warning restore CS0618
                }

                if (resolvedPage != null)
                {
                    resolvedPage.Activate(page);
                    this.Page = resolvedPage;
                    this.LastDiagnosticMessage = $"Resolved to {resolvedPage.GetType().Name}";
                }
                else
                {
                    this.LastDiagnosticMessage = $"Base URL matched but no page pattern matched for '{page.Url}'";
                }
            }
            else
            {
                this.LastDiagnosticMessage =
                    $"Base URL did not match (level: {result.Level}). URL: '{page.Url}'";
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
