using Microsoft.Playwright;
using System.Threading.Tasks;
using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Pages.Interfaces
{
    /// <summary>
    /// Interface for pages that implement custom async matching logic.
    /// MatchablePage instances are checked BEFORE regular UnionPage instances during URL routing.
    /// </summary>
    public interface IMatchablePage : IUnionPage
    {
        /// <summary>
        /// Performs async matching including optional DOM checks.
        /// Default implementation calls UriMatcher; overrides can call base.MatchAsync()
        /// then add DOM verification.
        /// </summary>
        /// <param name="requestData">The request URL data to match against.</param>
        /// <param name="baseUrlInfo">The base URL information for the service.</param>
        /// <param name="playwrightPage">The Playwright page for DOM checks. Use zero/immediate timeout for detection.</param>
        /// <returns>Match result with Success flag and optional Reason for diagnostics.</returns>
        ValueTask<UriMatchResult> MatchAsync(RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage);
    }
}
