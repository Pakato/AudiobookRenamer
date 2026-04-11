using System;
using System.Net;
using System.Net.Http;
using Goodreads.Scraper.Configuration;
using Goodreads.Scraper.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Goodreads.Scraper.Extensions
{
    /// <summary>
    /// Extension methods for configuring Goodreads Scraper services in DI container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Goodreads Scraper services to the DI container with proper HttpClient configuration,
        /// Polly retry policies, and all required dependencies.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureSettings">Optional action to configure scraper settings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddGoodreadsScraper(
            this IServiceCollection services,
            Action<GoodreadsScraperSettings>? configureSettings = null)
        {
            // Register settings
            var settings = new GoodreadsScraperSettings();
            configureSettings?.Invoke(settings);

            services.AddSingleton(Options.Create(settings));

            // Register helper services
            services.AddSingleton(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<GoodreadsScraperSettings>>();
                return new UserAgentRotator(opts.Value.CustomUserAgents);
            });

            services.AddSingleton(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<GoodreadsScraperSettings>>();
                return new ProxyRotator(opts.Value.Proxy?.RotatingProxies);
            });

            // Configure HttpClient with IHttpClientFactory
            services.AddHttpClient<IGoodreadsScraperService, GoodreadsScraperService>(
                (sp, client) =>
                {
                    var opts = sp.GetRequiredService<IOptions<GoodreadsScraperSettings>>().Value;
                    client.BaseAddress = new Uri(opts.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
                    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                })
                .ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var opts = sp.GetRequiredService<IOptions<GoodreadsScraperSettings>>().Value;
                    return CreateHttpHandler(opts);
                })
                .AddPolicyHandler((sp, _) =>
                {
                    var opts = sp.GetRequiredService<IOptions<GoodreadsScraperSettings>>().Value;
                    return CreateRetryPolicy(opts);
                })
                .AddPolicyHandler(GetCircuitBreakerPolicy());

            return services;
        }

        /// <summary>
        /// Adds Goodreads Scraper services using configuration section binding.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddGoodreadsScraper(
            this IServiceCollection services,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            // Bind configuration
            services.Configure<GoodreadsScraperSettings>(
                configuration.GetSection(GoodreadsScraperSettings.SectionName));

            // Register helper services
            services.AddSingleton(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<GoodreadsScraperSettings>>();
                return new UserAgentRotator(opts.Value.CustomUserAgents);
            });

            services.AddSingleton(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<GoodreadsScraperSettings>>();
                return new ProxyRotator(opts.Value.Proxy?.RotatingProxies);
            });

            // Configure HttpClient with IHttpClientFactory
            services.AddHttpClient<IGoodreadsScraperService, GoodreadsScraperService>(
                (sp, client) =>
                {
                    var opts = sp.GetRequiredService<IOptions<GoodreadsScraperSettings>>().Value;
                    client.BaseAddress = new Uri(opts.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
                    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                })
                .ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var opts = sp.GetRequiredService<IOptions<GoodreadsScraperSettings>>().Value;
                    return CreateHttpHandler(opts);
                })
                .AddPolicyHandler((sp, _) =>
                {
                    var opts = sp.GetRequiredService<IOptions<GoodreadsScraperSettings>>().Value;
                    return CreateRetryPolicy(opts);
                })
                .AddPolicyHandler(GetCircuitBreakerPolicy());

            return services;
        }

        /// <summary>
        /// Creates a SocketsHttpHandler with proper configuration including proxy support.
        /// Using SocketsHttpHandler prevents socket exhaustion issues.
        /// </summary>
        private static SocketsHttpHandler CreateHttpHandler(GoodreadsScraperSettings settings)
        {
            var handler = new SocketsHttpHandler
            {
                // Connection pooling settings to prevent socket exhaustion
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 10,

                // Enable automatic decompression
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,

                // Allow auto-redirect
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5,

                // Use cookies
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };

            // Configure proxy if enabled
            if (settings.UseProxy && settings.Proxy != null)
            {
                var proxy = settings.Proxy.CreateWebProxy();
                if (proxy != null)
                {
                    handler.Proxy = proxy;
                    handler.UseProxy = true;
                }
            }

            return handler;
        }

        /// <summary>
        /// Creates a Polly retry policy with exponential backoff and jitter.
        /// Handles transient HTTP errors, timeouts, and rate limiting (429).
        /// </summary>
        private static AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicy(GoodreadsScraperSettings settings)
        {
            var jitter = new Random();

            return HttpPolicyExtensions
                .HandleTransientHttpError() // Handles 5xx and 408
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests) // Handle 429
                .WaitAndRetryAsync(
                    retryCount: settings.MaxRetries,
                    sleepDurationProvider: (retryAttempt, response, context) =>
                    {
                        // Check for Retry-After header
                        if (response.Result?.Headers.RetryAfter?.Delta.HasValue == true)
                        {
                            return response.Result.Headers.RetryAfter.Delta.Value;
                        }

                        // Exponential backoff with jitter
                        // Formula: baseDelay * 2^attempt + random jitter (0-1 second)
                        var exponentialDelay = TimeSpan.FromSeconds(
                            Math.Pow(2, retryAttempt) * settings.RetryBaseDelaySeconds);

                        var jitterMs = jitter.Next(0, 1000);

                        return exponentialDelay + TimeSpan.FromMilliseconds(jitterMs);
                    },
                    onRetryAsync: async (outcome, timespan, retryAttempt, context) =>
                    {
                        // Log retry attempts (if logger available in context)
                        context["RetryAttempt"] = retryAttempt;
                        context["WaitTime"] = timespan;

                        // If it's a 429, wait a bit longer
                        if (outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5));
                        }
                    });
        }

        /// <summary>
        /// Creates a circuit breaker policy to prevent cascading failures.
        /// Opens the circuit after consecutive failures and allows recovery.
        /// </summary>
        private static Polly.CircuitBreaker.AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromMinutes(1));
        }
    }
}
