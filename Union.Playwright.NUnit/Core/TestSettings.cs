namespace Union.Playwright.NUnit.Core
{
    public class TestSettings
    {
        public static TestSettings Default { get; } = new TestSettings();

        /// <summary>
        /// Maximum time (ms) to wait for page resolution after navigation.
        /// Useful for SPAs that perform client-side redirects after initial load.
        /// Set to 0 to disable retry. Default: 5000ms.
        /// </summary>
        public int NavigationResolveTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Interval (ms) between page resolution attempts during the retry period.
        /// Default: 250ms.
        /// </summary>
        public int NavigationPollIntervalMs { get; set; } = 250;

        /// <summary>
        /// Timeout in milliseconds for WaitLoadedAsync after navigation.
        /// Default: 30000 (30 seconds). Set to 0 to disable timeout.
        /// </summary>
        public int WaitLoadedTimeoutMs { get; set; } = 30000;
    }
}
