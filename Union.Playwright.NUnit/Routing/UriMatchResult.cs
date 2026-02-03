using Microsoft.Playwright;
using System.Collections.Generic;

namespace Union.Playwright.NUnit.Routing
{
    public class UriMatchResult
    {
        #region Properties
        public bool Success { get; private set; }
        public Dictionary<string, string> Data { get; private set; }
        public Dictionary<string, string> Params { get; private set; }
        public List<Cookie> Cookies { get; private set; }

        /// <summary>
        /// Optional diagnostic message explaining why the match succeeded or failed.
        /// </summary>
        public string? Reason { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a match result with all properties.
        /// Prefer using static factory methods Matched() or Unmatched().
        /// </summary>
        public UriMatchResult(
            bool success,
            Dictionary<string, string>? data = null,
            Dictionary<string, string>? queryParams = null,
            List<Cookie>? cookies = null,
            string? reason = null)
        {
            this.Success = success;
            this.Data = data ?? new Dictionary<string, string>();
            this.Params = queryParams ?? new Dictionary<string, string>();
            this.Cookies = cookies ?? new List<Cookie>();
            this.Reason = reason;
        }
        #endregion

        #region Static Factory Methods
        /// <summary>
        /// Creates a successful match result with extracted data.
        /// </summary>
        public static UriMatchResult Matched(
            Dictionary<string, string>? data = null,
            Dictionary<string, string>? queryParams = null,
            List<Cookie>? cookies = null)
            => new(true, data, queryParams, cookies);

        /// <summary>
        /// Creates a successful match result with optional reason.
        /// </summary>
        public static UriMatchResult Matched(string? reason)
            => new(success: true, reason: reason);

        /// <summary>
        /// Creates a failed match result with optional reason.
        /// </summary>
        public static UriMatchResult Unmatched(string? reason = null)
            => new(success: false, reason: reason);
        #endregion
    }
}
