using System;
using System.Collections.Generic;
using System.Net;

namespace Goodreads.Scraper.Configuration
{
    /// <summary>
    /// Configuration settings for the Goodreads Web Scraper.
    /// </summary>
    public sealed class GoodreadsScraperSettings
    {
        /// <summary>
        /// Configuration section name for IOptions binding.
        /// </summary>
        public const string SectionName = "GoodreadsScraper";

        /// <summary>
        /// Base URL for Goodreads.
        /// </summary>
        public string BaseUrl { get; set; } = "https://www.goodreads.com";

        /// <summary>
        /// Delay between requests in milliseconds to avoid rate limiting.
        /// Default: 2000ms (2 seconds).
        /// </summary>
        public int RequestDelayMs { get; set; } = 2000;

        /// <summary>
        /// Maximum number of retry attempts for failed requests.
        /// Default: 3.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Base delay for exponential backoff in seconds.
        /// Default: 2 seconds.
        /// </summary>
        public int RetryBaseDelaySeconds { get; set; } = 2;

        /// <summary>
        /// Request timeout in seconds.
        /// Default: 30 seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Whether to use proxy rotation.
        /// </summary>
        public bool UseProxy { get; set; } = false;

        /// <summary>
        /// Proxy configuration settings.
        /// </summary>
        public ProxySettings? Proxy { get; set; }

        /// <summary>
        /// Maximum number of search results to return.
        /// Default: 10.
        /// </summary>
        public int MaxSearchResults { get; set; } = 10;

        /// <summary>
        /// Custom User-Agent strings for rotation.
        /// If empty, default User-Agents will be used.
        /// </summary>
        public List<string> CustomUserAgents { get; set; } = [];
    }

    /// <summary>
    /// Proxy configuration settings.
    /// </summary>
    public sealed class ProxySettings
    {
        /// <summary>
        /// Proxy server address (e.g., "http://proxy.example.com:8080").
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Proxy username for authentication.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Proxy password for authentication.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Whether to bypass proxy for local addresses.
        /// </summary>
        public bool BypassOnLocal { get; set; } = true;

        /// <summary>
        /// List of proxy addresses for rotation.
        /// </summary>
        public List<ProxyEndpoint> RotatingProxies { get; set; } = [];

        /// <summary>
        /// Creates a WebProxy instance from settings.
        /// </summary>
        public WebProxy? CreateWebProxy()
        {
            if (string.IsNullOrWhiteSpace(Address))
                return null;

            var proxy = new WebProxy(Address)
            {
                BypassProxyOnLocal = BypassOnLocal
            };

            if (!string.IsNullOrWhiteSpace(Username))
            {
                proxy.Credentials = new NetworkCredential(Username, Password);
            }

            return proxy;
        }
    }

    /// <summary>
    /// Represents a single proxy endpoint for rotation.
    /// </summary>
    public sealed class ProxyEndpoint
    {
        /// <summary>
        /// Proxy address.
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Optional username.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Optional password.
        /// </summary>
        public string? Password { get; set; }
    }
}
