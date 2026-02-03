using Microsoft.Playwright;
using System.Collections.Generic;

namespace Union.Playwright.NUnit.Routing
{
    public class UriMatchResult
    {
        public UriMatchResult(bool success)
            : this(success, new Dictionary<string, string>())
        {
        }

        public UriMatchResult(bool success, string? reason)
            : this(success, new Dictionary<string, string>(), new Dictionary<string, string>(), new List<Cookie>(), reason)
        {
        }

        public UriMatchResult(bool success, Dictionary<string, string> data)
            : this(success, data, new Dictionary<string, string>())
        {
        }

        public UriMatchResult(bool success, Dictionary<string, string> data, Dictionary<string, string> _params)
            : this(success, data, _params, new List<Cookie>())
        {
        }

        public UriMatchResult(
            bool success,
            Dictionary<string, string> data,
            Dictionary<string, string> _params,
            List<Cookie> cookies)
            : this(success, data, _params, cookies, null)
        {
        }

        public UriMatchResult(
            bool success,
            Dictionary<string, string> data,
            Dictionary<string, string> _params,
            List<Cookie> cookies,
            string? reason)
        {
            Success = success;
            Data = data;
            Cookies = cookies;
            Params = _params;
            Reason = reason;
        }

        public bool Success { get; private set; }

        public Dictionary<string, string> Data { get; private set; }

        public List<Cookie> Cookies { get; private set; }

        public Dictionary<string, string> Params { get; set; }

        /// <summary>
        /// Optional diagnostic message explaining why the match succeeded or failed.
        /// </summary>
        public string? Reason { get; private set; }

        public static UriMatchResult Matched(string? reason = null)
        {
            return new UriMatchResult(true, reason);
        }

        public static UriMatchResult Unmatched(string? reason = null)
        {
            return new UriMatchResult(false, reason);
        }
    }
}