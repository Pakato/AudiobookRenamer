using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AngleSharp;
using AngleSharp.Dom;
using Goodreads.Scraper.Configuration;
using Goodreads.Scraper.Http;
using Goodreads.Scraper.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace Goodreads.Scraper
{
    /// <summary>
    /// Web scraper service for extracting audiobook metadata from Goodreads using PuppeteerSharp.
    /// Uses a headless browser to handle JavaScript-rendered content and anti-bot protections.
    /// </summary>
    public sealed class GoodreadsPuppeteerScraperService : IGoodreadsScraperService, IAsyncDisposable, IDisposable
    {
        private readonly ILogger<GoodreadsPuppeteerScraperService> _logger;
        private readonly GoodreadsScraperSettings _settings;
        private readonly UserAgentRotator _userAgentRotator;
        private readonly IBrowsingContext _browsingContext;
        private readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);
        private readonly SemaphoreSlim _browserSemaphore = new(1, 1);
        private DateTime _lastRequestTime = DateTime.MinValue;
        private IBrowser? _browser;
        private bool _browserDownloaded;
        private bool _disposed;

        private static readonly Regex PageCountRegexPattern = new(@"(\d+)(?:\s*-\s*(\d+))?\s*(?:page|pages|p\.?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SeriesNumberRegexPattern = new(@"#(\d+(?:\.\d+)?)", RegexOptions.Compiled);
        private static readonly Regex SeriesNameRegexPattern = new(@"\(([^)]+?),?\s*#\d+(?:\.\d+)?\)", RegexOptions.Compiled);
        private static readonly Regex BookIdRegexPattern = new(@"/book/show/(\d+)", RegexOptions.Compiled);
        private static readonly Regex YearRegexPattern = new(@"(\d{4})", RegexOptions.Compiled);

        public GoodreadsPuppeteerScraperService(
            IOptions<GoodreadsScraperSettings> settings,
            ILogger<GoodreadsPuppeteerScraperService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _userAgentRotator = new UserAgentRotator(_settings.CustomUserAgents);
            var config = AngleSharp.Configuration.Default.WithDefaultLoader();
            _browsingContext = BrowsingContext.New(config);
        }

        private async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken = default)
        {
            if (_browser != null && _browser.IsConnected)
                return _browser;

            await _browserSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_browser != null && _browser.IsConnected)
                    return _browser;

                if (!_browserDownloaded)
                {
                    _logger.LogInformation("Downloading Chromium browser...");
                    var browserFetcher = new BrowserFetcher();
                    await browserFetcher.DownloadAsync();
                    _browserDownloaded = true;
                    _logger.LogInformation("Chromium browser downloaded successfully.");
                }

                _logger.LogInformation("Launching headless browser...");
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage", "--disable-accelerated-2d-canvas", "--disable-gpu", "--window-size=1920,1080"]
                });

                return _browser;
            }
            finally
            {
                _browserSemaphore.Release();
            }
        }

        private async Task<string> FetchPageAsync(string url, CancellationToken cancellationToken = default)
        {
            await EnforceRateLimitAsync(cancellationToken);
            var browser = await GetBrowserAsync(cancellationToken);
            await using var page = await browser.NewPageAsync();

            try
            {
                var userAgent = _userAgentRotator.GetNextUserAgent();
                await page.SetUserAgentAsync(userAgent);
                await page.SetViewportAsync(new ViewPortOptions { Width = 1920, Height = 1080 });

                var headers = UserAgentRotator.GetBrowserHeaders(userAgent);
                await page.SetExtraHttpHeadersAsync(headers.ToDictionary(h => h.Key, h => h.Value));

                _logger.LogDebug("Navigating to: {Url}", url);

                var response = await page.GoToAsync(url, new NavigationOptions
                {
                    WaitUntil = [WaitUntilNavigation.Networkidle2],
                    Timeout = _settings.TimeoutSeconds * 1000
                });

                if (response == null || !response.Ok)
                {
                    _logger.LogWarning("Failed to load page: {Url}, Status: {Status}", url, response?.Status);
                    return string.Empty;
                }

                try
                {
                    await page.WaitForSelectorAsync(".BookPage__mainContent, .leftContainer, .bookTitle, .SearchResults",
                        new WaitForSelectorOptions { Timeout = 10000 });
                }
                catch (WaitTaskTimeoutException)
                {
                    _logger.LogDebug("Timeout waiting for specific selectors, continuing with current content");
                }

                await Task.Delay(500, cancellationToken);
                var html = await page.GetContentAsync();
                _logger.LogDebug("Successfully fetched page: {Url} ({Length} chars)", url, html.Length);
                return html;
            }
            catch (NavigationException ex)
            {
                _logger.LogWarning(ex, "Navigation failed for: {Url}", url);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching page: {Url}", url);
                throw;
            }
        }

        private async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
        {
            await _rateLimitSemaphore.WaitAsync(cancellationToken);
            try
            {
                var elapsed = DateTime.UtcNow - _lastRequestTime;
                var delay = TimeSpan.FromMilliseconds(_settings.RequestDelayMs) - elapsed;
                if (delay > TimeSpan.Zero)
                {
                    _logger.LogDebug("Rate limiting: waiting {Delay}ms", delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                }
                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
        }

        public async Task<IReadOnlyList<GoodreadsSearchResult>> SearchBooksAsync(string query, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(query);
            _logger.LogInformation("Searching Goodreads (Puppeteer) for: {Query}", query);

            var encodedQuery = HttpUtility.UrlEncode(query);
            var searchUrl = $"{_settings.BaseUrl}/search?q={encodedQuery}&search_type=books";
            var html = await FetchPageAsync(searchUrl, cancellationToken);

            if (string.IsNullOrEmpty(html))
            {
                _logger.LogWarning("Empty response from search");
                return [];
            }

            return await ParseSearchResultsAsync(html, cancellationToken);
        }

        public async Task<AudiobookMetadata?> GetBookMetadataAsync(string bookId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(bookId);
            _logger.LogInformation("Fetching metadata (Puppeteer) for book ID: {BookId}", bookId);

            var bookUrl = $"{_settings.BaseUrl}/book/show/{bookId}";
            var html = await FetchPageAsync(bookUrl, cancellationToken);

            if (string.IsNullOrEmpty(html))
            {
                _logger.LogWarning("Empty response for book: {BookId}", bookId);
                return null;
            }

            return await ParseBookMetadataAsync(html, bookId, cancellationToken);
        }

        public async Task<AudiobookMetadata?> SearchAndGetMetadataAsync(string query, CancellationToken cancellationToken = default)
        {
            var results = await SearchBooksAsync(query, cancellationToken);
            if (results.Count == 0) return null;

            var firstResult = results[0];
            if (string.IsNullOrEmpty(firstResult.BookId)) return null;

            return await GetBookMetadataAsync(firstResult.BookId, cancellationToken);
        }

        public async Task<byte[]?> DownloadCoverImageAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return null;

            try
            {
                await EnforceRateLimitAsync(cancellationToken);
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgentRotator.GetNextUserAgent());
                var imageData = await httpClient.GetByteArrayAsync(imageUrl, cancellationToken);
                _logger.LogDebug("Downloaded cover image: {Size} bytes", imageData.Length);
                return imageData;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download cover image: {Url}", imageUrl);
                return null;
            }
        }

        private async Task<IReadOnlyList<GoodreadsSearchResult>> ParseSearchResultsAsync(string html, CancellationToken cancellationToken)
        {
            var results = new List<GoodreadsSearchResult>();
            using var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);

            var bookElements = document.QuerySelectorAll("tr[itemtype='http://schema.org/Book']")
                .Concat(document.QuerySelectorAll(".tableList tr"))
                .Concat(document.QuerySelectorAll("[data-testid='searchResult']"))
                .Take(_settings.MaxSearchResults);

            foreach (var bookElement in bookElements)
            {
                try
                {
                    var result = ParseSearchResultElement(bookElement);
                    if (result != null && !string.IsNullOrEmpty(result.BookId))
                    {
                        results.Add(result);
                        _logger.LogDebug("Found: {Title} by {Authors}", result.Title, string.Join(", ", result.Authors));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error parsing search result element");
                }
            }

            _logger.LogInformation("Found {Count} search results", results.Count);
            return results;
        }

        private GoodreadsSearchResult? ParseSearchResultElement(IElement element)
        {
            var result = new GoodreadsSearchResult();
            var linkElement = element.QuerySelector("a.bookTitle") ?? element.QuerySelector("a[href*='/book/show/']");
            if (linkElement == null) return null;

            var href = linkElement.GetAttribute("href") ?? "";
            var idMatch = BookIdRegexPattern.Match(href);
            if (!idMatch.Success) return null;

            result.BookId = idMatch.Groups[1].Value;
            result.BookUrl = href.StartsWith("http") ? href : $"{_settings.BaseUrl}{href}";
            result.Title = linkElement.TextContent?.Trim() ?? "";

            var authorElements = element.QuerySelectorAll("a.authorName, .authorName a");
            result.Authors = authorElements.Select(a => a.TextContent?.Trim() ?? "").Where(a => !string.IsNullOrEmpty(a)).ToList();

            var ratingElement = element.QuerySelector(".minirating, .ratings, [data-testid='rating']");
            if (ratingElement != null)
            {
                var ratingText = ratingElement.TextContent ?? "";
                var ratingMatch = Regex.Match(ratingText, @"(\d+\.?\d*)");
                if (ratingMatch.Success && decimal.TryParse(ratingMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rating))
                    result.Rating = rating;
            }

            var imgElement = element.QuerySelector("img");
            if (imgElement != null)
                result.CoverImageUrl = imgElement.GetAttribute("src") ?? imgElement.GetAttribute("data-src");

            var yearElement = element.QuerySelector(".greyText, .publicationDate");
            if (yearElement != null)
            {
                var yearMatch = YearRegexPattern.Match(yearElement.TextContent ?? "");
                if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var year))
                    result.Year = year;
            }

            return result;
        }

        private async Task<AudiobookMetadata?> ParseBookMetadataAsync(string html, string bookId, CancellationToken cancellationToken)
        {
            using var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);

            var metadata = new AudiobookMetadata
            {
                GoodreadsId = bookId,
                GoodreadsUrl = $"{_settings.BaseUrl}/book/show/{bookId}"
            };

            var titleElement = document.QuerySelector("h1[data-testid='bookTitle']") ?? document.QuerySelector("h1.Text__title1") ?? document.QuerySelector("#bookTitle") ?? document.QuerySelector("h1.bookTitle");
            metadata.Title = titleElement?.TextContent?.Trim() ?? "";

            var seriesElement = document.QuerySelector("h3.Text__title3 a[href*='/series/']") ?? document.QuerySelector("#bookSeries a") ?? document.QuerySelector("a[href*='/series/']");
            if (seriesElement != null)
            {
                var seriesText = seriesElement.TextContent?.Trim() ?? "";
                var seriesMatch = SeriesNameRegexPattern.Match(seriesText);
                metadata.Series = (seriesMatch.Success ? seriesMatch.Groups[1].Value.Trim() : seriesText.Trim('(', ')', ' ')).Split([':', '#'], 2)[0].Trim();

                var numberMatch = SeriesNumberRegexPattern.Match(seriesText);
                if (numberMatch.Success) metadata.SeriesNumber = numberMatch.Groups[1].Value;
            }
            else if (!string.IsNullOrEmpty(metadata.Title))
            {
                // Try to extract series info from title
                var title = metadata.Title;

                // First, check if there's a colon - everything before it is the series name
                // Example: "Series Name: Book 1" or "Series Name: Subtitle #1"
                var colonIndex = title.IndexOf(':');
                string seriesCandidate = colonIndex > 0 ? title[..colonIndex].Trim() : title;

                // Try to find series number in the full title
                string? seriesNumber = null;

                // Pattern 1: Look for #X or Book X pattern anywhere in title
                var numberMatch = Regex.Match(title, @"(?:#|Book\s*#?)(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                if (numberMatch.Success)
                {
                    seriesNumber = numberMatch.Groups[1].Value;
                }
                else
                {
                    // Pattern 2: Look for number in parentheses at end: "(1)" or "( 1 )"
                    var parenNumberMatch = Regex.Match(title, @"\(\s*(\d+(?:\.\d+)?)\s*\)\s*$");
                    if (parenNumberMatch.Success)
                    {
                        seriesNumber = parenNumberMatch.Groups[1].Value;
                    }
                    else
                    {
                        // Pattern 3: Fallback - first number found in the string
                        var firstNumberMatch = Regex.Match(title, @"(\d+(?:\.\d+)?)");
                        if (firstNumberMatch.Success)
                        {
                            seriesNumber = firstNumberMatch.Groups[1].Value;
                        }
                    }
                }

                // If we found a series number, extract the series name
                if (!string.IsNullOrEmpty(seriesNumber))
                {
                    metadata.SeriesNumber = seriesNumber;

                    // Check if there's content in parentheses that might be the series name
                    var parenMatch = Regex.Match(title, @"\(([^)]+)\)");
                    if (parenMatch.Success)
                    {
                        var parenContent = parenMatch.Groups[1].Value;
                        // Remove number patterns from parentheses content
                        var cleanedSeries = Regex.Replace(parenContent, @",?\s*(?:#|Book\s*#?)?\d+(?:\.\d+)?\s*$", "", RegexOptions.IgnoreCase).Trim();
                        if (!string.IsNullOrEmpty(cleanedSeries))
                        {
                            metadata.Series = cleanedSeries;
                        }
                        else
                        {
                            // Parentheses only had a number, use the part before colon as series
                            metadata.Series = seriesCandidate;
                        }
                    }
                    else
                    {
                        // No parentheses, use the part before colon (or full title if no colon)
                        // Clean up the series name by removing any trailing number patterns
                        var cleanedSeries = Regex.Replace(seriesCandidate, @",?\s*(?:#|Book\s*#?)?\d+(?:\.\d+)?\s*$", "", RegexOptions.IgnoreCase).Trim();
                        metadata.Series = !string.IsNullOrEmpty(cleanedSeries) ? cleanedSeries : seriesCandidate;
                    }
                }
            }


            var authorElements = document.QuerySelectorAll("span.ContributorLink__name, a.authorName span[itemprop='name'], .authorName a");
            metadata.Authors = authorElements.Select(a => a.TextContent?.Trim() ?? "").Where(a => !string.IsNullOrEmpty(a)).Distinct().ToList();

            var ratingElement = document.QuerySelector("div.RatingStatistics__rating") ?? document.QuerySelector("span[itemprop='ratingValue']") ?? document.QuerySelector(".average");
            if (ratingElement != null && decimal.TryParse(ratingElement.TextContent?.Trim() ?? "", NumberStyles.Any, CultureInfo.InvariantCulture, out var rating))
                metadata.Rating = rating;

            var ratingsCountElement = document.QuerySelector("span[data-testid='ratingsCount']") ?? document.QuerySelector("meta[itemprop='ratingCount']") ?? document.QuerySelector(".ratingsCount");
            if (ratingsCountElement != null)
            {
                var countText = ratingsCountElement.GetAttribute("content") ?? ratingsCountElement.TextContent ?? "";
                var countMatch = Regex.Match(countText.Replace(",", "").Replace(".", ""), @"(\d+)");
                if (countMatch.Success && int.TryParse(countMatch.Groups[1].Value, out var count))
                    metadata.RatingsCount = count;
            }

            var descriptionElement = document.QuerySelector("div.DetailsLayoutRightParagraph__widthConstrained span.Formatted") ?? document.QuerySelector("div[data-testid='description'] span.Formatted") ?? document.QuerySelector("#description span") ?? document.QuerySelector(".BookPageMetadataSection__description span");
            metadata.Description = descriptionElement?.TextContent?.Trim();

            var genreElements = document.QuerySelectorAll("span.BookPageMetadataSection__genreButton a, .bookPageGenreLink, a[href*='/genres/']");
            metadata.Genres = genreElements.Select(g => g.TextContent?.Trim() ?? "").Where(g => !string.IsNullOrEmpty(g) && !g.Contains("...more")).Distinct().Take(10).ToList();

            var publicationElement = document.QuerySelector("p[data-testid='publicationInfo']") ?? document.QuerySelector("#details .row");
            if (publicationElement != null)
            {
                var pubText = publicationElement.TextContent ?? "";
                var yearMatch = YearRegexPattern.Match(pubText);
                if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var year))
                    metadata.Year = year;

                var publisherMatch = Regex.Match(pubText, @"by\s+([^,\n]+)", RegexOptions.IgnoreCase);
                if (publisherMatch.Success) metadata.Publisher = publisherMatch.Groups[1].Value.Trim();
            }

            var pagesElement = document.QuerySelector("p[data-testid='pagesFormat']") ?? document.QuerySelector("span[itemprop='numberOfPages']") ?? document.QuerySelector("#details .pages");
            if (pagesElement != null)
            {
                var pagesMatch = PageCountRegexPattern.Match(pagesElement.TextContent ?? "");
                if (pagesMatch.Success && int.TryParse(pagesMatch.Groups[1].Value, out var pages))
                    metadata.PageCount = pages;
            }

            var isbnElement = document.QuerySelector("span[itemprop='isbn']") ?? document.QuerySelector("div.infoBoxRowItem[itemprop='isbn']");
            if (isbnElement != null)
            {
                var isbn = isbnElement.TextContent?.Trim() ?? "";
                if (isbn.Length == 13) metadata.Isbn13 = isbn;
                else if (isbn.Length == 10) metadata.Isbn = isbn;
            }

            var coverElement = document.QuerySelector("img.ResponsiveImage") ?? document.QuerySelector("img#coverImage") ?? document.QuerySelector(".BookCover__image img");
            if (coverElement != null)
            {
                var coverUrl = coverElement.GetAttribute("src") ?? coverElement.GetAttribute("data-src");
                if (!string.IsNullOrEmpty(coverUrl))
                {
                    metadata.CoverImageUrl = coverUrl.Replace("._SX98_", "").Replace("._SY160_", "").Replace("._SX50_", "").Replace("._SY75_", "");
                    metadata.SmallCoverImageUrl = coverUrl;
                }
            }

            var languageElement = document.QuerySelector("div[itemprop='inLanguage']") ?? document.QuerySelector(".infoBoxRowItem[itemprop='inLanguage']");
            metadata.Language = languageElement?.TextContent?.Trim();

            _logger.LogInformation("Parsed metadata for: {Title} by {Author}", metadata.Title, metadata.PrimaryAuthor);
            return metadata;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            if (_browser != null)
            {
                try
                {
                    await _browser.CloseAsync();
                    _browser.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing browser");
                }
            }

            _rateLimitSemaphore.Dispose();
            _browserSemaphore.Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _browser?.Dispose();
            _rateLimitSemaphore.Dispose();
            _browserSemaphore.Dispose();
        }
    }
}
