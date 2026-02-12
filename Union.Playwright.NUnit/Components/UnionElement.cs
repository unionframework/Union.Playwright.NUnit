using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Components
{
    public class UnionElement : ComponentBase, ILocator
    {
        public UnionElement(IUnionPage parentPage, string rootScss)
            : base(parentPage, rootScss)
        {
            if (string.IsNullOrWhiteSpace(rootScss))
                throw new ArgumentException("Selector must not be null or empty.", nameof(rootScss));
        }

        // Click-and-wait convenience methods
        public Task<TPage?> ClickAndWaitForRedirectAsync<TPage>() where TPage : class, IUnionPage
            => this.Action.ClickAndWaitForRedirectAsync<TPage>(this.RootLocator);

        public Task<TModal?> ClickAndWaitForAlertAsync<TModal>() where TModal : class, IUnionModal
            => this.Action.ClickAndWaitForAlertAsync<TModal>(this.RootLocator);

        public Task<TComponent> ClickAndWaitForAsync<TComponent>() where TComponent : ComponentBase
            => this.Action.ClickAndWaitForAsync<TComponent>(this.RootLocator);

        // Properties
        public IFrameLocator ContentFrame => this.RootLocator.ContentFrame;
        public ILocator First => this.RootLocator.First;
        public ILocator Last => this.RootLocator.Last;
        public IPage Page => this.RootLocator.Page;

        // Locator creation & filtering
        public ILocator And(ILocator locator) => this.RootLocator.And(locator);
        public ILocator Filter(LocatorFilterOptions? options = default) => this.RootLocator.Filter(options);
        public IFrameLocator FrameLocator(string selector) => this.RootLocator.FrameLocator(selector);
        public ILocator Locator(string selectorOrLocator, LocatorLocatorOptions? options = default)
            => this.RootLocator.Locator(selectorOrLocator, options);
        public ILocator Locator(ILocator selectorOrLocator, LocatorLocatorOptions? options = default)
            => this.RootLocator.Locator(selectorOrLocator, options);
        public ILocator Nth(int index) => this.RootLocator.Nth(index);
        public ILocator Or(ILocator locator) => this.RootLocator.Or(locator);

        // GetBy selectors
        public ILocator GetByAltText(string text, LocatorGetByAltTextOptions? options = default)
            => this.RootLocator.GetByAltText(text, options);
        public ILocator GetByAltText(Regex text, LocatorGetByAltTextOptions? options = default)
            => this.RootLocator.GetByAltText(text, options);
        public ILocator GetByLabel(string text, LocatorGetByLabelOptions? options = default)
            => this.RootLocator.GetByLabel(text, options);
        public ILocator GetByLabel(Regex text, LocatorGetByLabelOptions? options = default)
            => this.RootLocator.GetByLabel(text, options);
        public ILocator GetByPlaceholder(string text, LocatorGetByPlaceholderOptions? options = default)
            => this.RootLocator.GetByPlaceholder(text, options);
        public ILocator GetByPlaceholder(Regex text, LocatorGetByPlaceholderOptions? options = default)
            => this.RootLocator.GetByPlaceholder(text, options);
        public ILocator GetByRole(AriaRole role, LocatorGetByRoleOptions? options = default)
            => this.RootLocator.GetByRole(role, options);
        public ILocator GetByTestId(string testId) => this.RootLocator.GetByTestId(testId);
        public ILocator GetByTestId(Regex testId) => this.RootLocator.GetByTestId(testId);
        public ILocator GetByText(string text, LocatorGetByTextOptions? options = default)
            => this.RootLocator.GetByText(text, options);
        public ILocator GetByText(Regex text, LocatorGetByTextOptions? options = default)
            => this.RootLocator.GetByText(text, options);
        public ILocator GetByTitle(string text, LocatorGetByTitleOptions? options = default)
            => this.RootLocator.GetByTitle(text, options);
        public ILocator GetByTitle(Regex text, LocatorGetByTitleOptions? options = default)
            => this.RootLocator.GetByTitle(text, options);

        // Element interaction
        public Task BlurAsync(LocatorBlurOptions? options = default)
            => this.RootLocator.BlurAsync(options);
        public Task CheckAsync(LocatorCheckOptions? options = default)
            => this.RootLocator.CheckAsync(options);
        public Task ClearAsync(LocatorClearOptions? options = default)
            => this.RootLocator.ClearAsync(options);
        public Task ClickAsync(LocatorClickOptions? options = default)
            => this.RootLocator.ClickAsync(options);
        public Task DblClickAsync(LocatorDblClickOptions? options = default)
            => this.RootLocator.DblClickAsync(options);
        public Task DragToAsync(ILocator target, LocatorDragToOptions? options = default)
            => this.RootLocator.DragToAsync(target, options);
        public Task FillAsync(string value, LocatorFillOptions? options = default)
            => this.RootLocator.FillAsync(value, options);
        public Task FocusAsync(LocatorFocusOptions? options = default)
            => this.RootLocator.FocusAsync(options);
        public Task HoverAsync(LocatorHoverOptions? options = default)
            => this.RootLocator.HoverAsync(options);
        public Task PressAsync(string key, LocatorPressOptions? options = default)
            => this.RootLocator.PressAsync(key, options);
        public Task PressSequentiallyAsync(string text, LocatorPressSequentiallyOptions? options = default)
            => this.RootLocator.PressSequentiallyAsync(text, options);
        public Task ScrollIntoViewIfNeededAsync(LocatorScrollIntoViewIfNeededOptions? options = default)
            => this.RootLocator.ScrollIntoViewIfNeededAsync(options);
        public Task SetCheckedAsync(bool checkedState, LocatorSetCheckedOptions? options = default)
            => this.RootLocator.SetCheckedAsync(checkedState, options);
        public Task TapAsync(LocatorTapOptions? options = default)
            => this.RootLocator.TapAsync(options);
        public Task TypeAsync(string text, LocatorTypeOptions? options = default)
            => this.RootLocator.TypeAsync(text, options);
        public Task UncheckAsync(LocatorUncheckOptions? options = default)
            => this.RootLocator.UncheckAsync(options);

        // Select & file input
        public Task<IReadOnlyList<string>> SelectOptionAsync(string values, LocatorSelectOptionOptions? options = default)
            => this.RootLocator.SelectOptionAsync(values, options);
        public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<string> values, LocatorSelectOptionOptions? options = default)
            => this.RootLocator.SelectOptionAsync(values, options);
        public Task<IReadOnlyList<string>> SelectOptionAsync(IElementHandle values, LocatorSelectOptionOptions? options = default)
            => this.RootLocator.SelectOptionAsync(values, options);
        public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, LocatorSelectOptionOptions? options = default)
            => this.RootLocator.SelectOptionAsync(values, options);
        public Task<IReadOnlyList<string>> SelectOptionAsync(SelectOptionValue values, LocatorSelectOptionOptions? options = default)
            => this.RootLocator.SelectOptionAsync(values, options);
        public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<SelectOptionValue> values, LocatorSelectOptionOptions? options = default)
            => this.RootLocator.SelectOptionAsync(values, options);
        public Task SelectTextAsync(LocatorSelectTextOptions? options = default)
            => this.RootLocator.SelectTextAsync(options);
        public Task SetInputFilesAsync(string files, LocatorSetInputFilesOptions? options = default)
            => this.RootLocator.SetInputFilesAsync(files, options);
        public Task SetInputFilesAsync(IEnumerable<string> files, LocatorSetInputFilesOptions? options = default)
            => this.RootLocator.SetInputFilesAsync(files, options);
        public Task SetInputFilesAsync(FilePayload files, LocatorSetInputFilesOptions? options = default)
            => this.RootLocator.SetInputFilesAsync(files, options);
        public Task SetInputFilesAsync(IEnumerable<FilePayload> files, LocatorSetInputFilesOptions? options = default)
            => this.RootLocator.SetInputFilesAsync(files, options);

        // Content extraction
        public Task<IReadOnlyList<string>> AllInnerTextsAsync()
            => this.RootLocator.AllInnerTextsAsync();
        public Task<IReadOnlyList<string>> AllTextContentsAsync()
            => this.RootLocator.AllTextContentsAsync();
        public Task<string?> GetAttributeAsync(string name, LocatorGetAttributeOptions? options = default)
            => this.RootLocator.GetAttributeAsync(name, options);
        public Task<string> InnerHTMLAsync(LocatorInnerHTMLOptions? options = default)
            => this.RootLocator.InnerHTMLAsync(options);
        public Task<string> InnerTextAsync(LocatorInnerTextOptions? options = default)
            => this.RootLocator.InnerTextAsync(options);
        public Task<string> InputValueAsync(LocatorInputValueOptions? options = default)
            => this.RootLocator.InputValueAsync(options);
        public Task<string?> TextContentAsync(LocatorTextContentOptions? options = default)
            => this.RootLocator.TextContentAsync(options);

        // State checking
        public Task<bool> IsCheckedAsync(LocatorIsCheckedOptions? options = default)
            => this.RootLocator.IsCheckedAsync(options);
        public Task<bool> IsDisabledAsync(LocatorIsDisabledOptions? options = default)
            => this.RootLocator.IsDisabledAsync(options);
        public Task<bool> IsEditableAsync(LocatorIsEditableOptions? options = default)
            => this.RootLocator.IsEditableAsync(options);
        public Task<bool> IsEnabledAsync(LocatorIsEnabledOptions? options = default)
            => this.RootLocator.IsEnabledAsync(options);
        public Task<bool> IsHiddenAsync(LocatorIsHiddenOptions? options = default)
            => this.RootLocator.IsHiddenAsync(options);
        Task<bool> ILocator.IsVisibleAsync(LocatorIsVisibleOptions? options)
            => this.RootLocator.IsVisibleAsync(options);

        // Element queries
        public Task<IReadOnlyList<ILocator>> AllAsync()
            => this.RootLocator.AllAsync();
        public Task<int> CountAsync()
            => this.RootLocator.CountAsync();
        public Task<IElementHandle> ElementHandleAsync(LocatorElementHandleOptions? options = default)
            => this.RootLocator.ElementHandleAsync(options);
        public Task<IReadOnlyList<IElementHandle>> ElementHandlesAsync()
            => this.RootLocator.ElementHandlesAsync();

        // Evaluation & events
        public Task<JsonElement?> EvaluateAsync(string expression, object? arg = default, LocatorEvaluateOptions? options = default)
            => this.RootLocator.EvaluateAsync(expression, arg, options);
        public Task<T> EvaluateAsync<T>(string expression, object? arg = default, LocatorEvaluateOptions? options = default)
            => this.RootLocator.EvaluateAsync<T>(expression, arg, options);
        public Task<T> EvaluateAllAsync<T>(string expression, object? arg = default)
            => this.RootLocator.EvaluateAllAsync<T>(expression, arg);
        public Task<IJSHandle> EvaluateHandleAsync(string expression, object? arg = default, LocatorEvaluateHandleOptions? options = default)
            => this.RootLocator.EvaluateHandleAsync(expression, arg, options);
        public Task DispatchEventAsync(string type, object? eventInit = default, LocatorDispatchEventOptions? options = default)
            => this.RootLocator.DispatchEventAsync(type, eventInit, options);

        // Visual & debugging
        public Task<string> AriaSnapshotAsync(LocatorAriaSnapshotOptions? options = default)
            => this.RootLocator.AriaSnapshotAsync(options);
        public Task<LocatorBoundingBoxResult?> BoundingBoxAsync(LocatorBoundingBoxOptions? options = default)
            => this.RootLocator.BoundingBoxAsync(options);
        public Task HighlightAsync()
            => this.RootLocator.HighlightAsync();
        public Task<byte[]> ScreenshotAsync(LocatorScreenshotOptions? options = default)
            => this.RootLocator.ScreenshotAsync(options);
        public Task WaitForAsync(LocatorWaitForOptions? options = default)
            => this.RootLocator.WaitForAsync(options);
    }
}
