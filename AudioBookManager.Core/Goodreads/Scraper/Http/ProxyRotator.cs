using System;
using System.Collections.Generic;
using System.Net;
using Goodreads.Scraper.Configuration;

namespace Goodreads.Scraper.Http
{
    /// <summary>
    /// Manages rotation of proxy servers to avoid IP-based blocks.
    /// </summary>
    public sealed class ProxyRotator
    {
        private readonly List<ProxyEndpoint> _proxies;
        private int _currentIndex;
        private readonly object _lock = new();
        private static readonly Random _random = new();

        /// <summary>
        /// Creates a new proxy rotator with the specified endpoints.
        /// </summary>
        public ProxyRotator(IEnumerable<ProxyEndpoint>? proxyEndpoints = null)
        {
            _proxies = proxyEndpoints != null
                ? new List<ProxyEndpoint>(proxyEndpoints)
                : [];
            _currentIndex = _proxies.Count > 0 ? _random.Next(_proxies.Count) : 0;
        }

        /// <summary>
        /// Gets whether any proxies are configured.
        /// </summary>
        public bool HasProxies => _proxies.Count > 0;

        /// <summary>
        /// Adds a proxy endpoint to the rotation.
        /// </summary>
        public void AddProxy(ProxyEndpoint endpoint)
        {
            lock (_lock)
            {
                _proxies.Add(endpoint);
            }
        }

        /// <summary>
        /// Removes a proxy endpoint (e.g., if it's blocked).
        /// </summary>
        public bool RemoveProxy(string address)
        {
            lock (_lock)
            {
                var index = _proxies.FindIndex(p =>
                    p.Address.Equals(address, StringComparison.OrdinalIgnoreCase));

                if (index >= 0)
                {
                    _proxies.RemoveAt(index);
                    if (_currentIndex >= _proxies.Count)
                        _currentIndex = 0;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the next proxy in rotation.
        /// </summary>
        public WebProxy? GetNextProxy()
        {
            lock (_lock)
            {
                if (_proxies.Count == 0)
                    return null;

                var endpoint = _proxies[_currentIndex];
                _currentIndex = (_currentIndex + 1) % _proxies.Count;

                return CreateWebProxy(endpoint);
            }
        }

        /// <summary>
        /// Gets a random proxy from the pool.
        /// </summary>
        public WebProxy? GetRandomProxy()
        {
            lock (_lock)
            {
                if (_proxies.Count == 0)
                    return null;

                var endpoint = _proxies[_random.Next(_proxies.Count)];
                return CreateWebProxy(endpoint);
            }
        }

        private static WebProxy CreateWebProxy(ProxyEndpoint endpoint)
        {
            var proxy = new WebProxy(endpoint.Address)
            {
                BypassProxyOnLocal = true
            };

            if (!string.IsNullOrWhiteSpace(endpoint.Username))
            {
                proxy.Credentials = new NetworkCredential(endpoint.Username, endpoint.Password);
            }

            return proxy;
        }
    }
}
