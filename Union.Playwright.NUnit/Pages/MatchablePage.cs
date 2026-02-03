using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
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

        #region DOM Check Helpers

        /// <summary>
        /// Checks if an element exists without waiting. Uses immediate timeout.
        /// Use this for DOM-based page identification in MatchAsync overrides.
        /// </summary>
        /// <param name="page">The Playwright page to check.</param>
        /// <param name="selector">CSS selector for the element.</param>
        /// <returns>True if at least one element matches the selector.</returns>
        protected static async ValueTask<bool> ElementExistsAsync(IPage page, string selector)
        {
            try
            {
                var count = await page.Locator(selector).CountAsync();
                return count > 0;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (PlaywrightException)
            {
                // Element not found or page navigated away
                return false;
            }
        }

        /// <summary>
        /// Checks if an element contains specific text without waiting.
        /// Use this for DOM-based page identification in MatchAsync overrides.
        /// </summary>
        /// <param name="page">The Playwright page to check.</param>
        /// <param name="selector">CSS selector for the element.</param>
        /// <param name="expectedText">Text that the element should contain.</param>
        /// <returns>True if element exists and contains the expected text.</returns>
        protected static async ValueTask<bool> ElementContainsTextAsync(
            IPage page,
            string selector,
            string expectedText)
        {
            try
            {
                var locator = page.Locator(selector);
                if (await locator.CountAsync() == 0)
                    return false;

                var text = await locator.First.TextContentAsync();
                return text?.Contains(expectedText, StringComparison.OrdinalIgnoreCase) ?? false;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
