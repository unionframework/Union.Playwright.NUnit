using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Core
{
    public interface IBrowserState
    {
        IModalWindow? ModalWindow { get; }
        IUnionPage? Page { get; }
        string? LastActualizedUrl => null;
        string? LastDiagnosticMessage => null;

        /// <summary>
        /// Async actualization supporting MatchablePage with DOM checks.
        /// </summary>
        ValueTask ActualizeAsync(IPage page);

        /// <summary>
        /// Synchronous actualization. Does not support MatchablePage.
        /// </summary>
        [Obsolete("Use ActualizeAsync instead. This method does not support MatchablePage.")]
        void Actualize(IPage page);

        T? PageAs<T>() where T : class, IUnionPage => Page as T;
        bool PageIs<T>() where T : IUnionPage;
    }
}