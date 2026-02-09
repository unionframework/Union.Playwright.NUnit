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
            => this.Action.ClickAndWaitForRedirectAsync<TPage>(this.Root);

        public Task<TModal?> ClickAndWaitForAlertAsync<TModal>() where TModal : class, IUnionModal
            => this.Action.ClickAndWaitForAlertAsync<TModal>(this.Root);

        public Task<TComponent> ClickAndWaitForAsync<TComponent>() where TComponent : ComponentBase
            => this.Action.ClickAndWaitForAsync<TComponent>(this.Root);

        // Properties
        public IFrameLocator ContentFrame => this.Root.ContentFrame;
        public ILocator First => this.Root.First;
        public ILocator Last => this.Root.Last;
        public IPage Page => this.Root.Page;

        // Locator creation & filtering
        public ILocator And(ILocator locator) => this.Root.And(locator);
        public ILocator Filter(LocatorFilterOptions? options = default) => this.Root.Filter(options);
        public IFrameLocator FrameLocator(string selector) => this.Root.FrameLocator(selector);
        public ILocator Locator(string selectorOrLocator, LocatorLocatorOptions? options = default)
            => this.Root.Locator(selectorOrLocator, options);
        public ILocator Locator(ILocator selectorOrLocator, LocatorLocatorOptions? options = default)
            => this.Root.Locator(selectorOrLocator, options);
        public ILocator Nth(int index) => this.Root.Nth(index);
        public ILocator Or(ILocator locator) => this.Root.Or(locator);

        // GetBy selectors
        public ILocator GetByAltText(string text, LocatorGetByAltTextOptions? options = default)
            => this.Root.GetByAltText(text, options);
        public ILocator GetByAltText(Regex text, LocatorGetByAltTextOptions? options = default)
            => this.Root.GetByAltText(text, options);
        public ILocator GetByLabel(string text, LocatorGetByLabelOptions? options = default)
            => this.Root.GetByLabel(text, options);
        public ILocator GetByLabel(Regex text, LocatorGetByLabelOptions? options = default)
            => this.Root.GetByLabel(text, options);
        public ILocator GetByPlaceholder(string text, LocatorGetByPlaceholderOptions? options = default)
            => this.Root.GetByPlaceholder(text, options);
        public ILocator GetByPlaceholder(Regex text, LocatorGetByPlaceholderOptions? options = default)
            => this.Root.GetByPlaceholder(text, options);
        public ILocator GetByRole(AriaRole role, LocatorGetByRoleOptions? options = default)
            => this.Root.GetByRole(role, options);
        public ILocator GetByTestId(string testId) => this.Root.GetByTestId(testId);
        public ILocator GetByTestId(Regex testId) => this.Root.GetByTestId(testId);
        public ILocator GetByText(string text, LocatorGetByTextOptions? options = default)
            => this.Root.GetByText(text, options);
        public ILocator GetByText(Regex text, LocatorGetByTextOptions? options = default)
            => this.Root.GetByText(text, options);
        public ILocator GetByTitle(string text, LocatorGetByTitleOptions? options = default)
            => this.Root.GetByTitle(text, options);
        public ILocator GetByTitle(Regex text, LocatorGetByTitleOptions? options = default)
            => this.Root.GetByTitle(text, options);

        // Element interaction
        public Task BlurAsync(LocatorBlurOptions? options = default)
            => this.Root.BlurAsync(options);
        public Task CheckAsync(LocatorCheckOptions? options = default)
            => this.Root.CheckAsync(options);
        public Task ClearAsync(LocatorClearOptions? options = default)
            => this.Root.ClearAsync(options);
        public Task ClickAsync(LocatorClickOptions? options = default)
            => this.Root.ClickAsync(options);
        public Task DblClickAsync(LocatorDblClickOptions? options = default)
            => this.Root.DblClickAsync(options);
        public Task DragToAsync(ILocator target, LocatorDragToOptions? options = default)
            => this.Root.DragToAsync(target, options);
        public Task FillAsync(string value, LocatorFillOptions? options = default)
            => this.Root.FillAsync(value, options);
        public Task FocusAsync(LocatorFocusOptions? options = default)
            => this.Root.FocusAsync(options);
        public Task HoverAsync(LocatorHoverOptions? options = default)
            => this.Root.HoverAsync(options);
        public Task PressAsync(string key, LocatorPressOptions? options = default)
            => this.Root.PressAsync(key, options);
        public Task PressSequentiallyAsync(string text, LocatorPressSequentiallyOptions? options = default)
            => this.Root.PressSequentiallyAsync(text, options);
        public Task ScrollIntoViewIfNeededAsync(LocatorScrollIntoViewIfNeededOptions? options = default)
            => this.Root.ScrollIntoViewIfNeededAsync(options);
        public Task SetCheckedAsync(bool checkedState, LocatorSetCheckedOptions? options = default)
            => this.Root.SetCheckedAsync(checkedState, options);
        public Task TapAsync(LocatorTapOptions? options = default)
            => this.Root.TapAsync(options);
        public Task TypeAsync(string text, LocatorTypeOptions? options = default)
            => this.Root.TypeAsync(text, options);
        public Task UncheckAsync(LocatorUncheckOptions? options = default)
            => this.Root.UncheckAsync(options);

        // Select & file input
        public Task<IReadOnlyList<string>> SelectOptionAsync(string values, LocatorSelectOptionOptions? options = default)
            => this.Root.SelectOptionAsync(values, options);
        public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<string> values, LocatorSelectOptionOptions? options = default)
            => this.Root.SelectOptionAsync(values, options);
        public Task<IReadOnlyList<string>> SelectOptionAsync(IElementHandle values, LocatorSelectOptionOptions? options = default)
            => this.Root.SelectOptionAsync(values, options);
        public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, LocatorSelectOptionOptions? options = default)
            => this.Root.SelectOptionAsync(values, options);
        public Task<IReadOnlyList<string>> SelectOptionAsync(SelectOptionValue values, LocatorSelectOptionOptions? options = default)
            => this.Root.SelectOptionAsync(values, options);
        public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<SelectOptionValue> values, LocatorSelectOptionOptions? options = default)
            => this.Root.SelectOptionAsync(values, options);
        public Task SelectTextAsync(LocatorSelectTextOptions? options = default)
            => this.Root.SelectTextAsync(options);
        public Task SetInputFilesAsync(string files, LocatorSetInputFilesOptions? options = default)
            => this.Root.SetInputFilesAsync(files, options);
        public Task SetInputFilesAsync(IEnumerable<string> files, LocatorSetInputFilesOptions? options = default)
            => this.Root.SetInputFilesAsync(files, options);
        public Task SetInputFilesAsync(FilePayload files, LocatorSetInputFilesOptions? options = default)
            => this.Root.SetInputFilesAsync(files, options);
        public Task SetInputFilesAsync(IEnumerable<FilePayload> files, LocatorSetInputFilesOptions? options = default)
            => this.Root.SetInputFilesAsync(files, options);

        // Content extraction
        public Task<IReadOnlyList<string>> AllInnerTextsAsync()
            => this.Root.AllInnerTextsAsync();
        public Task<IReadOnlyList<string>> AllTextContentsAsync()
            => this.Root.AllTextContentsAsync();
        public Task<string?> GetAttributeAsync(string name, LocatorGetAttributeOptions? options = default)
            => this.Root.GetAttributeAsync(name, options);
        public Task<string> InnerHTMLAsync(LocatorInnerHTMLOptions? options = default)
            => this.Root.InnerHTMLAsync(options);
        public Task<string> InnerTextAsync(LocatorInnerTextOptions? options = default)
            => this.Root.InnerTextAsync(options);
        public Task<string> InputValueAsync(LocatorInputValueOptions? options = default)
            => this.Root.InputValueAsync(options);
        public Task<string?> TextContentAsync(LocatorTextContentOptions? options = default)
            => this.Root.TextContentAsync(options);

        // State checking
        public Task<bool> IsCheckedAsync(LocatorIsCheckedOptions? options = default)
            => this.Root.IsCheckedAsync(options);
        public Task<bool> IsDisabledAsync(LocatorIsDisabledOptions? options = default)
            => this.Root.IsDisabledAsync(options);
        public Task<bool> IsEditableAsync(LocatorIsEditableOptions? options = default)
            => this.Root.IsEditableAsync(options);
        public Task<bool> IsEnabledAsync(LocatorIsEnabledOptions? options = default)
            => this.Root.IsEnabledAsync(options);
        public Task<bool> IsHiddenAsync(LocatorIsHiddenOptions? options = default)
            => this.Root.IsHiddenAsync(options);
        Task<bool> ILocator.IsVisibleAsync(LocatorIsVisibleOptions? options)
            => this.Root.IsVisibleAsync(options);

        // Element queries
        public Task<IReadOnlyList<ILocator>> AllAsync()
            => this.Root.AllAsync();
        public Task<int> CountAsync()
            => this.Root.CountAsync();
        public Task<IElementHandle> ElementHandleAsync(LocatorElementHandleOptions? options = default)
            => this.Root.ElementHandleAsync(options);
        public Task<IReadOnlyList<IElementHandle>> ElementHandlesAsync()
            => this.Root.ElementHandlesAsync();

        // Evaluation & events
        public Task<JsonElement?> EvaluateAsync(string expression, object? arg = default, LocatorEvaluateOptions? options = default)
            => this.Root.EvaluateAsync(expression, arg, options);
        public Task<T> EvaluateAsync<T>(string expression, object? arg = default, LocatorEvaluateOptions? options = default)
            => this.Root.EvaluateAsync<T>(expression, arg, options);
        public Task<T> EvaluateAllAsync<T>(string expression, object? arg = default)
            => this.Root.EvaluateAllAsync<T>(expression, arg);
        public Task<IJSHandle> EvaluateHandleAsync(string expression, object? arg = default, LocatorEvaluateHandleOptions? options = default)
            => this.Root.EvaluateHandleAsync(expression, arg, options);
        public Task DispatchEventAsync(string type, object? eventInit = default, LocatorDispatchEventOptions? options = default)
            => this.Root.DispatchEventAsync(type, eventInit, options);

        // Visual & debugging
        public Task<string> AriaSnapshotAsync(LocatorAriaSnapshotOptions? options = default)
            => this.Root.AriaSnapshotAsync(options);
        public Task<LocatorBoundingBoxResult?> BoundingBoxAsync(LocatorBoundingBoxOptions? options = default)
            => this.Root.BoundingBoxAsync(options);
        public Task HighlightAsync()
            => this.Root.HighlightAsync();
        public Task<byte[]> ScreenshotAsync(LocatorScreenshotOptions? options = default)
            => this.Root.ScreenshotAsync(options);
        public Task WaitForAsync(LocatorWaitForOptions? options = default)
            => this.Root.WaitForAsync(options);
    }
}
