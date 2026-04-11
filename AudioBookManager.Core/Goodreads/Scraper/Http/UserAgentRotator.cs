using System;
using System.Collections.Generic;

namespace Goodreads.Scraper.Http
{
    /// <summary>
    /// Provides rotation of User-Agent strings to mimic real browsers
    /// and avoid detection as a bot.
    /// </summary>
    public sealed class UserAgentRotator
    {
        private static readonly Random _random = new();
        private readonly List<string> _userAgents;
        private int _currentIndex;
        private readonly object _lock = new();

        /// <summary>
        /// Default User-Agent strings representing various browsers and platforms.
        /// Updated to reflect current browser versions (2024/2025).
        /// </summary>
        private static readonly List<string> DefaultUserAgents =
        [
            // Chrome on Windows 11
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36",

            // Chrome on macOS
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",

            // Firefox on Windows
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:132.0) Gecko/20100101 Firefox/132.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:131.0) Gecko/20100101 Firefox/131.0",

            // Firefox on macOS
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:133.0) Gecko/20100101 Firefox/133.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 14.5; rv:132.0) Gecko/20100101 Firefox/132.0",

            // Safari on macOS
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_5) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Safari/605.1.15",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15",

            // Edge on Windows
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0",

            // Chrome on Linux
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (X11; Ubuntu; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",

            // Firefox on Linux
            "Mozilla/5.0 (X11; Linux x86_64; rv:133.0) Gecko/20100101 Firefox/133.0",
            "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:132.0) Gecko/20100101 Firefox/132.0"
        ];

        /// <summary>
        /// Creates a new User-Agent rotator with default or custom agents.
        /// </summary>
        /// <param name="customUserAgents">Optional custom User-Agent list.</param>
        public UserAgentRotator(IEnumerable<string>? customUserAgents = null)
        {
            _userAgents = customUserAgents != null && customUserAgents is List<string> list && list.Count > 0
                ? list
                : DefaultUserAgents;

            // Start at a random position
            _currentIndex = _random.Next(_userAgents.Count);
        }

        /// <summary>
        /// Gets a random User-Agent string.
        /// </summary>
        public string GetRandomUserAgent()
        {
            return _userAgents[_random.Next(_userAgents.Count)];
        }

        /// <summary>
        /// Gets the next User-Agent string in rotation (round-robin).
        /// </summary>
        public string GetNextUserAgent()
        {
            lock (_lock)
            {
                var userAgent = _userAgents[_currentIndex];
                _currentIndex = (_currentIndex + 1) % _userAgents.Count;
                return userAgent;
            }
        }

        /// <summary>
        /// Gets common browser headers to accompany requests.
        /// </summary>
        public static Dictionary<string, string> GetBrowserHeaders(string userAgent)
        {
            return new Dictionary<string, string>
            {
                ["User-Agent"] = userAgent,
                ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8",
                ["Accept-Language"] = "en-US,en;q=0.9",
                ["Accept-Encoding"] = "gzip, deflate, br",
                ["Cache-Control"] = "no-cache",
                ["Pragma"] = "no-cache",
                ["Sec-Ch-Ua"] = "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"",
                ["Sec-Ch-Ua-Mobile"] = "?0",
                ["Sec-Ch-Ua-Platform"] = "\"Windows\"",
                ["Sec-Fetch-Dest"] = "document",
                ["Sec-Fetch-Mode"] = "navigate",
                ["Sec-Fetch-Site"] = "none",
                ["Sec-Fetch-User"] = "?1",
                ["Upgrade-Insecure-Requests"] = "1"
            };
        }
    }
}
