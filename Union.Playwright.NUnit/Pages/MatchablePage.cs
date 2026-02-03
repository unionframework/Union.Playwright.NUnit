using Microsoft.Playwright;
using System.Threading.Tasks;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Pages
{
    /// <summary>
    /// Base class for pages that require custom async matching logic (e.g., DOM element checks).
    /// MatchablePage instances are evaluated BEFORE regular UnionPage instances during routing.
    ///
    /// <para>
    /// Override MatchAsync() to add DOM checks:
    /// <code>
    /// public override async ValueTask&lt;UriMatchResult&gt; MatchAsync(
    ///     RequestData requestData, BaseUrlInfo baseUrlInfo, IPage playwrightPage)
    /// {
    ///     var baseResult = await base.MatchAsync(requestData, baseUrlInfo, playwrightPage);
    ///     if (!baseResult.Success)
    ///         return baseResult;
    ///
    ///     // DOM check with zero timeout (detection, not waiting)
    ///     var element = playwrightPage.Locator(".company-selector");
    ///     var count = await element.CountAsync();
    ///     if (count == 0)
    ///         return UriMatchResult.Unmatched("Company selector not found in DOM");
    ///
    ///     return baseResult;
    /// }
    /// </code>
    /// </para>
    /// </summary>
    /// <remarks>
    /// TODO: Future enhancement - support direct navigation to MatchablePage via Go.ToPage&lt;T&gt;()
    /// that navigates to AbsolutePath and waits until MatchAsync() succeeds.
    /// Currently, MatchablePage is primarily for URL-based routing detection after navigation.
    /// </remarks>
    public abstract class MatchablePage : UnionPage, IMatchablePage
    {
        /// <summary>
        /// Default implementation uses UriMatcher for URL pattern matching.
        /// Override to add DOM checks (call base.MatchAsync() first, then verify DOM elements).
        /// </summary>
        /// <param name="requestData">The request URL data to match against.</param>
        /// <param name="baseUrlInfo">The base URL information for the service.</param>
        /// <param name="playwrightPage">The Playwright page for DOM checks. Use zero/immediate timeout for detection.</param>
        /// <returns>Match result with Success flag and optional Reason for diagnostics.</returns>
        public virtual ValueTask<UriMatchResult> MatchAsync(
            RequestData requestData,
            BaseUrlInfo baseUrlInfo,
            IPage playwrightPage)
        {
            var matcher = new UriMatcher(AbsolutePath, Data, Params);
            var result = matcher.Match(requestData.Url, baseUrlInfo.AbsolutePath);
            return ValueTask.FromResult(result);
        }

        /// <summary>
        /// Override to customize request data generation for navigation.
        /// </summary>
        public new virtual RequestData GetRequest(BaseUrlInfo defaultBaseUrlInfo)
        {
            return base.GetRequest(defaultBaseUrlInfo);
        }
    }
}
