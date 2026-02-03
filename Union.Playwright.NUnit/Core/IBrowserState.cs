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

        /// <summary>
        /// Gets the URL from the last actualization attempt.
        /// Implementations should set this in Actualize/ActualizeAsync.
        /// Default returns null for backward compatibility.
        /// </summary>
        string? LastActualizedUrl => null;

        /// <summary>
        /// Gets diagnostic information from the last actualization attempt.
        /// </summary>
        string? LastDiagnosticMessage { get; }

        /// <summary>
        /// Appends additional diagnostic information to the current message.
        /// Used by navigation components to add timeout/resolution context.
        /// </summary>
        void AppendDiagnosticMessage(string additionalInfo) { }

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