using Microsoft.Playwright;

namespace Union.Playwright.NUnit.Extensions;

public static class PageExtensions
{
    /// <summary>
    /// Waits for the page to reach a stable state by checking network idle and URL stability.
    /// Replaces arbitrary WaitForTimeoutAsync calls with deterministic waiting.
    /// </summary>
    public static async Task WaitForPageStableAsync(
        this IPage page,
        float timeoutMs = 10_000,
        int stabilityDelayMs = 250
    )
    {
        await page.WaitForLoadStateAsync(
            LoadState.NetworkIdle,
            new() { Timeout = timeoutMs }
        );

        // URL stability check: capture URL, wait, confirm unchanged
        var url = page.Url;
        await page.WaitForTimeoutAsync(stabilityDelayMs);

        if (page.Url != url)
        {
            // URL changed during stability window - wait for network idle again
            await page.WaitForLoadStateAsync(
                LoadState.NetworkIdle,
                new() { Timeout = timeoutMs }
            );
        }
    }

    /// <summary>
    /// Resilient wrapper around Page.EvaluateAsync that retries on execution context destruction.
    /// Drop-in replacement for Page.EvaluateAsync when navigations may destroy the context.
    /// </summary>
    public static async Task<T> EvaluateSafeAsync<T>(
        this IPage page,
        string expression,
        int maxRetries = 3,
        float timeoutMs = 10_000
    )
    {
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await page.EvaluateAsync<T>(expression);
            }
            catch (PlaywrightException ex) when (attempt < maxRetries && IsContextDestroyedException(ex))
            {
                await page.WaitForLoadStateAsync(
                    LoadState.DOMContentLoaded,
                    new() { Timeout = timeoutMs }
                );
            }
        }

        // Unreachable, but satisfies the compiler
        throw new InvalidOperationException("Exhausted retries in EvaluateSafeAsync");
    }

    private static bool IsContextDestroyedException(PlaywrightException ex)
    {
        var message = ex.Message;
        return message.Contains("context was destroyed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("navigating frame was detached", StringComparison.OrdinalIgnoreCase);
    }
}
