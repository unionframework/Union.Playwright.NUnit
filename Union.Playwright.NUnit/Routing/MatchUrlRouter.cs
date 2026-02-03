using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Routing
{
    // TODO: router should be a singleton
    public class MatchUrlRouter : IRouter
    {
        // Separate registries - MatchablePage checked FIRST
        private readonly List<IMatchablePage> _matchablePages;
        private readonly Dictionary<Type, IUnionPage> _regularPages;

        /// <summary>
        /// Diagnostic message from the last GetPageAsync call.
        /// </summary>
        public string? LastMatchReason { get; private set; }

        /// <summary>
        /// Contains warning message from last registration operation, if any.
        /// </summary>
        public string? LastRegistrationWarning { get; private set; }

        public MatchUrlRouter()
        {
            _matchablePages = new List<IMatchablePage>();
            _regularPages = new Dictionary<Type, IUnionPage>();
        }

        public RequestData GetRequest(IUnionPage page, BaseUrlInfo defaultBaseUrlInfo)
        {
            return page.GetRequest(defaultBaseUrlInfo);
        }

        /// <summary>
        /// Async page resolution. Checks MatchablePage instances FIRST (in registration order),
        /// then falls back to regular UnionPage instances.
        /// </summary>
        public async ValueTask<IUnionPage?> GetPageAsync(
            RequestData requestData,
            BaseUrlInfo baseUrlInfo,
            IPage playwrightPage)
        {
            LastMatchReason = null;

            // PHASE 1: Check MatchablePage instances first (priority)
            foreach (var dummyPage in _matchablePages)
            {
                // MatchAsync may throw - let exceptions propagate (not swallowed)
                var match = await dummyPage.MatchAsync(requestData, baseUrlInfo, playwrightPage);

                if (match.Success)
                {
                    var instance = CreatePageInstance<IMatchablePage>(dummyPage.GetType());
                    instance.BaseUrlInfo = baseUrlInfo;
                    instance.Data = match.Data;
                    instance.Params = match.Params;
                    instance.Cookies = match.Cookies;
                    LastMatchReason = match.Reason ?? $"Matched MatchablePage: {dummyPage.GetType().Name}";
                    return instance;
                }
                else if (match.Reason != null)
                {
                    // Track the reason even for failures (for diagnostics)
                    LastMatchReason = match.Reason;
                }
            }

            // PHASE 2: Fall back to regular UnionPage matching (sync)
            foreach (var dummyPage in _regularPages.Values)
            {
                var matcher = new UriMatcher(dummyPage.AbsolutePath, dummyPage.Data, dummyPage.Params);
                var match = matcher.Match(requestData.Url, baseUrlInfo.AbsolutePath);
                if (match.Success)
                {
                    var instance = CreatePageInstance<IUnionPage>(dummyPage.GetType());
                    instance.BaseUrlInfo = baseUrlInfo;
                    instance.Data = match.Data;
                    instance.Params = match.Params;
                    instance.Cookies = match.Cookies;
                    LastMatchReason = $"Matched UnionPage: {dummyPage.GetType().Name}";
                    return instance;
                }
            }

            LastMatchReason = "No page matched the URL";
            return null;
        }

        /// <summary>
        /// Synchronous page resolution (deprecated - does not support MatchablePage).
        /// Kept for backwards compatibility.
        /// </summary>
        [Obsolete("Use GetPageAsync instead. This method does not support MatchablePage.")]
        public IUnionPage? GetPage(RequestData requestData, BaseUrlInfo baseUrlInfo)
        {
            // Only check regular pages (MatchablePage requires async)
            foreach (var dummyPage in _regularPages.Values)
            {
                var matcher = new UriMatcher(dummyPage.AbsolutePath, dummyPage.Data, dummyPage.Params);
                var match = matcher.Match(requestData.Url, baseUrlInfo.AbsolutePath);
                if (match.Success)
                {
                    var instance = CreatePageInstance<IUnionPage>(dummyPage.GetType());
                    instance.BaseUrlInfo = baseUrlInfo;
                    instance.Data = match.Data;
                    instance.Params = match.Params;
                    instance.Cookies = match.Cookies;
                    return instance;
                }
            }
            return null;
        }

        public IReadOnlyList<Type> GetPageTypes()
        {
            var types = new List<Type>();
            types.AddRange(_matchablePages.Select(p => p.GetType()));
            types.AddRange(_regularPages.Keys);
            return types.AsReadOnly();
        }

        public bool HasPage(IUnionPage page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            var pageType = page.GetType();
            if (page is IMatchablePage)
            {
                return _matchablePages.Any(p => p.GetType() == pageType);
            }
            return _regularPages.ContainsKey(pageType);
        }

        public void RegisterPage<T>()
        {
            RegisterPage(typeof(T));
        }

        public void RegisterPage(Type pageType)
        {
            if (typeof(IMatchablePage).IsAssignableFrom(pageType))
            {
                RegisterMatchablePage(pageType);
            }
            else
            {
                RegisterRegularPage(pageType);
            }
        }

        private void RegisterMatchablePage(Type pageType)
        {
            this.LastRegistrationWarning = null;
            var pageInstance = CreatePageInstance<IMatchablePage>(pageType);

            // Check for duplicate among matchable pages (warning, not error - first match wins)
            var existingDuplicate = _matchablePages.FirstOrDefault(p =>
                p.AbsolutePath == pageInstance.AbsolutePath && p.GetType() != pageType);
            if (existingDuplicate != null)
            {
                // Allow duplicate paths for MatchablePage - first match wins based on registration order
                // This is intentional for scenarios like LoginPage and CompanySelectionPage sharing /login
                var warningMessage =
                    $"[Warning] MatchablePage '{pageType.Name}' has same path '{pageInstance.AbsolutePath}' " +
                    $"as existing MatchablePage '{existingDuplicate.GetType().Name}'. " +
                    $"First registered page will be checked first during matching.";
                this.LastRegistrationWarning = warningMessage;
                System.Diagnostics.Debug.WriteLine(warningMessage);
            }

            if (!_matchablePages.Any(p => p.GetType() == pageType))
            {
                _matchablePages.Add(pageInstance);
            }
        }

        private void RegisterRegularPage(Type pageType)
        {
            var pageInstance = CreatePageInstance<IUnionPage>(pageType);

            // Check for duplicate path among regular pages (error)
            var duplicate = _regularPages.FirstOrDefault(p =>
                p.Value.AbsolutePath == pageInstance.AbsolutePath && p.Key != pageType);
            if (duplicate.Key != null)
            {
                throw new InvalidOperationException(
                    $"Duplicate page path '{pageInstance.AbsolutePath}' registered by both " +
                    $"{duplicate.Key.Name} and {pageType.Name}.");
            }

            // Also check if this path conflicts with a MatchablePage
            var matchableConflict = _matchablePages.FirstOrDefault(p => p.AbsolutePath == pageInstance.AbsolutePath);
            if (matchableConflict != null)
            {
                // This is allowed - MatchablePage will be checked first and can decline the match
            }

            _regularPages[pageType] = pageInstance;
        }

        public void RegisterDerivedPages<T>()
        {
            var basePageType = typeof(T);
            var derivedPageTypes = Assembly.GetAssembly(basePageType)!
                .GetTypes()
                .Where(t => !t.IsAbstract && basePageType.IsAssignableFrom(t));
            foreach (var derivedPageType in derivedPageTypes)
            {
                RegisterPage(derivedPageType);
            }
        }

        /// <summary>
        /// Creates an instance of a page type with meaningful error messages for common failures.
        /// </summary>
        /// <typeparam name="T">The expected base type of the page instance.</typeparam>
        /// <param name="pageType">The concrete page type to instantiate.</param>
        /// <returns>A new instance of the page type.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when instantiation fails due to missing constructor, constructor exception, or type loading issues.
        /// </exception>
        private static T CreatePageInstance<T>(Type pageType) where T : class, IUnionPage
        {
            try
            {
                var instance = Activator.CreateInstance(pageType);
                if (instance == null)
                {
                    throw new InvalidOperationException(
                        $"Activator.CreateInstance returned null for type '{pageType.FullName}'.");
                }
                return (T)instance;
            }
            catch (MissingMethodException ex)
            {
                throw new InvalidOperationException(
                    $"Page type '{pageType.Name}' must have a public parameterless constructor. " +
                    $"Add a public constructor with no parameters to this class.", ex);
            }
            catch (TargetInvocationException ex)
            {
                throw new InvalidOperationException(
                    $"Constructor of page type '{pageType.Name}' threw an exception. " +
                    $"Page constructors should not throw exceptions.", ex.InnerException ?? ex);
            }
            catch (TypeLoadException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to load page type '{pageType.Name}'. " +
                    $"Ensure all dependencies are available.", ex);
            }
        }
    }
}