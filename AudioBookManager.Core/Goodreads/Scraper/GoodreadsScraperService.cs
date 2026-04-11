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
using AngleSharp.Html.Dom;
using Goodreads.Scraper.Configuration;
using Goodreads.Scraper.Http;
using Goodreads.Scraper.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Goodreads.Scraper
{
    /// <summary>
    /// Web scraper service for extracting audiobook metadata from Goodreads.
    /// Uses AngleSharp for HTML parsing and implements rate limiting, retry logic,
    /// and anti-bot evasion techniques.
    /// </summary>
    /// <remarks>
    /// FALLBACK NOTE: If Goodreads implements heavy JavaScript-based protection
    /// (CAPTCHAs, dynamic content loading), consider using PuppeteerSharp as a fallback:
    /// 
    /// // Install: dotnet add package PuppeteerSharp
    /// // Usage:
    /// // using var browserFetcher = new BrowserFetcher();
    /// // await browserFetcher.DownloadAsync();
    /// // await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
    /// // await using var page = await browser.NewPageAsync();
    /// // await page.GoToAsync(url);
    /// // var html = await page.GetContentAsync();
    /// </remarks>
    public sealed partial class GoodreadsScraperService : IGoodreadsScraperService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoodreadsScraperService> _logger;
        private readonly GoodreadsScraperSettings _settings;
        private readonly UserAgentRotator _userAgentRotator;
        private readonly IBrowsingContext _browsingContext;
        private readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);
        private DateTime _lastRequestTime = DateTime.MinValue;

        // Regex patterns for parsing
        [GeneratedRegex(@"(\d+)(?:\s*-\s*(\d+))?\s*(?:page|pages|p\.?)", RegexOptions.IgnoreCase)]
        private static partial Regex PageCountRegex();

        [GeneratedRegex(@"#(\d+(?:\.\d+)?)", RegexOptions.None)]
        private static partial Regex SeriesNumberRegex();

        [GeneratedRegex(@"\(([^)]+?),?\s*#\d+(?:\.\d+)?\)", RegexOptions.None)]
        private static partial Regex SeriesNameRegex();

        [GeneratedRegex(@"/book/show/(\d+)", RegexOptions.None)]
        private static partial Regex BookIdRegex();

        [GeneratedRegex(@"(\d{4})", RegexOptions.None)]
        private static partial Regex YearRegex();

        public GoodreadsScraperService(
            HttpClient httpClient,
            IOptions<GoodreadsScraperSettings> settings,
            ILogger<GoodreadsScraperService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

            _userAgentRotator = new UserAgentRotator(_settings.CustomUserAgents);

            // Configure AngleSharp for HTML5 parsing
            var config = AngleSharp.Configuration.Default
                .WithDefaultLoader();
            _browsingContext = BrowsingContext.New(config);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<GoodreadsSearchResult>> SearchBooksAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(query);

            _logger.LogInformation("Searching Goodreads for: {Query}", query);

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

        /// <inheritdoc/>
        public async Task<AudiobookMetadata?> GetBookMetadataAsync(
            string bookId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(bookId);

            _logger.LogInformation("Fetching metadata for book ID: {BookId}", bookId);

            var bookUrl = $"{_settings.BaseUrl}/book/show/{bookId}";
            var html = await FetchPageAsync(bookUrl, cancellationToken);

            if (string.IsNullOrEmpty(html))
            {
                _logger.LogWarning("Empty response for book {BookId}", bookId);
                return null;
            }

            return await ParseBookPageAsync(html, bookId, bookUrl, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<AudiobookMetadata?> SearchAndGetMetadataAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            var searchResults = await SearchBooksAsync(query, cancellationToken);

            if (searchResults.Count == 0)
            {
                _logger.LogInformation("No search results found for: {Query}", query);
                return null;
            }

            var bestMatch = searchResults[0];
            _logger.LogInformation("Best match: {Title} by {Authors}",
                bestMatch.Title,
                string.Join(", ", bestMatch.Authors));

            return await GetBookMetadataAsync(bestMatch.BookId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<byte[]?> DownloadCoverImageAsync(
            string imageUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;

            try
            {
                // Try to get high-resolution image URL
                var highResUrl = GetHighResolutionImageUrl(imageUrl);

                _logger.LogDebug("Downloading cover image from: {Url}", highResUrl);

                await EnforceRateLimitAsync(cancellationToken);

                var request = new HttpRequestMessage(HttpMethod.Get, highResUrl);
                ApplyBrowserHeaders(request);

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync(cancellationToken);
                }

                // Fallback to original URL if high-res fails
                if (highResUrl != imageUrl)
                {
                    request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
                    ApplyBrowserHeaders(request);
                    response = await _httpClient.SendAsync(request, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    }
                }

                _logger.LogWarning("Failed to download cover image: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading cover image from {Url}", imageUrl);
                return null;
            }
        }

        private async Task<string?> FetchPageAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                await EnforceRateLimitAsync(cancellationToken);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                ApplyBrowserHeaders(request);

                _logger.LogDebug("Fetching: {Url}", url);

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Rate limited (429). The Polly retry policy will handle this.");
                    throw new HttpRequestException("Rate limited", null, System.Net.HttpStatusCode.TooManyRequests);
                }

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Page not found: {Url}", url);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching page: {Url}", url);
                throw;
            }
        }

        private async Task<IReadOnlyList<GoodreadsSearchResult>> ParseSearchResultsAsync(
            string html,
            CancellationToken cancellationToken)
        {
            var results = new List<GoodreadsSearchResult>();

            using var document = await _browsingContext.OpenAsync(
                req => req.Content(html),
                cancellationToken);

            // Try modern layout selectors first
            var bookItems = document.QuerySelectorAll("tr[itemtype='http://schema.org/Book']");

            // Fallback to alternative selectors
            if (bookItems.Length == 0)
            {
                bookItems = document.QuerySelectorAll(".tableList tr");
            }

            foreach (var item in bookItems.Take(_settings.MaxSearchResults))
            {
                try
                {
                    var result = ParseSearchResultItem(item);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing search result item");
                }
            }

            _logger.LogInformation("Found {Count} search results", results.Count);
            return results;
        }

        private GoodreadsSearchResult? ParseSearchResultItem(IElement item)
        {
            // Get book link and ID
            var bookLink = item.QuerySelector("a.bookTitle") ??
                          item.QuerySelector("[itemprop='url']") ??
                          item.QuerySelector("a[href*='/book/show/']");

            if (bookLink == null)
                return null;

            var href = bookLink.GetAttribute("href") ?? string.Empty;
            var bookIdMatch = BookIdRegex().Match(href);
            if (!bookIdMatch.Success)
                return null;

            var result = new GoodreadsSearchResult
            {
                BookId = bookIdMatch.Groups[1].Value,
                BookUrl = href.StartsWith("http")
                    ? href
                    : $"{_settings.BaseUrl}{href}"
            };

            // Title
            var titleElement = item.QuerySelector("[itemprop='name']") ??
                              item.QuerySelector(".bookTitle span") ??
                              bookLink;
            result.Title = CleanText(titleElement?.TextContent);

            // Authors
            var authorElements = item.QuerySelectorAll("[itemprop='author'] [itemprop='name']");
            if (authorElements.Length == 0)
            {
                authorElements = item.QuerySelectorAll(".authorName span[itemprop='name']");
            }
            if (authorElements.Length == 0)
            {
                authorElements = item.QuerySelectorAll(".authorName");
            }

            foreach (var author in authorElements)
            {
                var authorName = CleanText(author.TextContent);
                if (!string.IsNullOrEmpty(authorName))
                {
                    result.Authors.Add(authorName);
                }
            }

            // Cover image
            var coverImg = item.QuerySelector("img.bookCover") ??
                          item.QuerySelector("img[src*='goodreads']");
            result.CoverImageUrl = coverImg?.GetAttribute("src");

            // Rating
            var ratingElement = item.QuerySelector(".minirating") ??
                               item.QuerySelector("[itemprop='ratingValue']");
            if (ratingElement != null)
            {
                var ratingText = ratingElement.TextContent;
                var ratingMatch = Regex.Match(ratingText, @"(\d+\.?\d*)");
                if (ratingMatch.Success && decimal.TryParse(
                    ratingMatch.Groups[1].Value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var rating))
                {
                    result.Rating = rating;
                }

                // Ratings count
                var countMatch = Regex.Match(ratingText, @"([\d,]+)\s*rating");
                if (countMatch.Success)
                {
                    var countStr = countMatch.Groups[1].Value.Replace(",", "");
                    if (int.TryParse(countStr, out var count))
                    {
                        result.RatingsCount = count;
                    }
                }
            }

            // Publication year
            var publishedElement = item.QuerySelector(".greyText.smallText");
            if (publishedElement != null)
            {
                var yearMatch = YearRegex().Match(publishedElement.TextContent);
                if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var year))
                {
                    result.Year = year;
                }
            }

            return result;
        }

        private async Task<AudiobookMetadata> ParseBookPageAsync(
            string html,
            string bookId,
            string bookUrl,
            CancellationToken cancellationToken)
        {
            using var document = await _browsingContext.OpenAsync(
                req => req.Content(html),
                cancellationToken);

            var metadata = new AudiobookMetadata
            {
                GoodreadsId = bookId,
                GoodreadsUrl = bookUrl
            };

            // Title
            var titleElement = document.QuerySelector("h1[data-testid='bookTitle']") ??
                              document.QuerySelector("h1.Text__title1") ??
                              document.QuerySelector("#bookTitle") ??
                              document.QuerySelector("h1[itemprop='name']");
            metadata.Title = CleanText(titleElement?.TextContent) ?? string.Empty;

            // Series info (often in title or separate element)
            ParseSeriesInfo(document, metadata);

            // Authors
            ParseAuthors(document, metadata);

            // Description
            var descElement = document.QuerySelector("[data-testid='description'] .Formatted") ??
                             document.QuerySelector("#description span[style*='display:none']") ??
                             document.QuerySelector("#description span:not(.actionLinkLite)") ??
                             document.QuerySelector("[itemprop='description']");
            metadata.Description = CleanHtml(descElement?.InnerHtml);

            // Cover image
            var coverImg = document.QuerySelector("img.ResponsiveImage") ??
                          document.QuerySelector("#coverImage") ??
                          document.QuerySelector("[itemprop='image']");
            metadata.CoverImageUrl = GetHighResolutionImageUrl(coverImg?.GetAttribute("src"));
            metadata.SmallCoverImageUrl = coverImg?.GetAttribute("src");

            // Rating
            var ratingElement = document.QuerySelector("[data-testid='RatingStatistics'] div.RatingStatistics__rating") ??
                               document.QuerySelector("[itemprop='ratingValue']") ??
                               document.QuerySelector(".RatingStatistics__rating");
            if (ratingElement != null && decimal.TryParse(
                CleanText(ratingElement.TextContent),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var rating))
            {
                metadata.Rating = rating;
            }

            // Ratings count
            var ratingsCountElement = document.QuerySelector("[data-testid='ratingsCount']") ??
                                     document.QuerySelector("[itemprop='ratingCount']");
            if (ratingsCountElement != null)
            {
                var countText = ratingsCountElement.GetAttribute("content") ??
                               ratingsCountElement.TextContent;
                var countMatch = Regex.Match(countText ?? "", @"([\d,]+)");
                if (countMatch.Success)
                {
                    var countStr = countMatch.Groups[1].Value.Replace(",", "");
                    if (int.TryParse(countStr, out var count))
                    {
                        metadata.RatingsCount = count;
                    }
                }
            }

            // Publication info
            ParsePublicationInfo(document, metadata);

            // Genres
            ParseGenres(document, metadata);

            // ISBN/ASIN
            ParseIdentifiers(document, metadata);

            // Page count
            ParsePageCount(document, metadata);

            // Language
            var langElement = document.QuerySelector("[itemprop='inLanguage']");
            metadata.Language = CleanText(langElement?.TextContent);

            _logger.LogInformation("Parsed metadata for: {Title} by {Authors}",
                metadata.Title,
                metadata.PrimaryAuthor);

            return metadata;
        }

        private void ParseSeriesInfo(IDocument document, AudiobookMetadata metadata)
        {
            // Try dedicated series element
            var seriesElement = document.QuerySelector("[data-testid='bookSeries'] a") ??
                               document.QuerySelector("h3.Text__title3 a[href*='/series/']") ??
                               document.QuerySelector("#bookSeries a");

            if (seriesElement != null)
            {
                var seriesText = CleanText(seriesElement.TextContent);
                if (!string.IsNullOrEmpty(seriesText))
                {
                    // Extract series name and number
                    var numberMatch = SeriesNumberRegex().Match(seriesText);
                    if (numberMatch.Success)
                    {
                        metadata.SeriesNumber = numberMatch.Groups[1].Value;
                        metadata.Series = seriesText
                            .Replace($"#{metadata.SeriesNumber}", "")
                            .Trim(' ', '(', ')', ',');
                    }
                    else
                    {
                        metadata.Series = seriesText.Trim(' ', '(', ')');
                    }
                }
            }
            else
            {
                // Try to extract from title
                var titleText = metadata.Title;
                var seriesMatch = SeriesNameRegex().Match(titleText);
                if (seriesMatch.Success)
                {
                    metadata.Series = seriesMatch.Groups[1].Value.Trim();
                    var numberMatch = SeriesNumberRegex().Match(titleText);
                    if (numberMatch.Success)
                    {
                        metadata.SeriesNumber = numberMatch.Groups[1].Value;
                    }
                    // Clean title
                    metadata.Title = SeriesNameRegex().Replace(metadata.Title, "").Trim();
                }
            }
        }

        private void ParseAuthors(IDocument document, AudiobookMetadata metadata)
        {
            // Modern layout
            var authorContainers = document.QuerySelectorAll("[data-testid='authorsList'] a[href*='/author/show/']");

            if (authorContainers.Length == 0)
            {
                // Legacy layout
                authorContainers = document.QuerySelectorAll("#bookAuthors a.authorName span[itemprop='name']");
            }

            if (authorContainers.Length == 0)
            {
                authorContainers = document.QuerySelectorAll(".authorName span[itemprop='name']");
            }

            foreach (var authorElement in authorContainers)
            {
                var authorName = CleanText(authorElement.TextContent);
                if (!string.IsNullOrEmpty(authorName) && !metadata.Authors.Contains(authorName))
                {
                    metadata.Authors.Add(authorName);
                }
            }

            // Try to find narrator for audiobooks
            var narratorElements = document.QuerySelectorAll("span:contains('Narrator'), span:contains('Narrated by')");
            foreach (var narratorSection in narratorElements)
            {
                var parent = narratorSection.ParentElement;
                var links = parent?.QuerySelectorAll("a");
                if (links != null)
                {
                    foreach (var link in links)
                    {
                        var name = CleanText(link.TextContent);
                        if (!string.IsNullOrEmpty(name) && !metadata.Narrators.Contains(name))
                        {
                            metadata.Narrators.Add(name);
                        }
                    }
                }
            }
        }

        private void ParsePublicationInfo(IDocument document, AudiobookMetadata metadata)
        {
            // Try to find publication details section
            var pubElement = document.QuerySelector("[data-testid='publicationInfo']") ??
                            document.QuerySelector("#details div.row");

            var pubText = pubElement?.TextContent ?? document.Body?.TextContent ?? "";

            // Publisher
            var publisherMatch = Regex.Match(pubText, @"(?:Published|Publisher)[:\s]+(?:by\s+)?([^,\d]+?)(?:,|\d|$)", RegexOptions.IgnoreCase);
            if (publisherMatch.Success)
            {
                metadata.Publisher = CleanText(publisherMatch.Groups[1].Value);
            }

            // Publication year
            var yearMatches = YearRegex().Matches(pubText);
            foreach (Match match in yearMatches)
            {
                if (int.TryParse(match.Groups[1].Value, out var year) && year >= 1800 && year <= DateTime.Now.Year + 1)
                {
                    if (pubText.Contains("first published", StringComparison.OrdinalIgnoreCase) ||
                        pubText.Contains("original", StringComparison.OrdinalIgnoreCase))
                    {
                        metadata.OriginalYear ??= year;
                    }
                    else
                    {
                        metadata.Year ??= year;
                    }
                }
            }

            // If only one year found, use it as publication year
            if (metadata.Year == null && metadata.OriginalYear != null)
            {
                metadata.Year = metadata.OriginalYear;
            }
        }

        private static void ParseGenres(IDocument document, AudiobookMetadata metadata)
        {
            // Modern layout - genre pills/buttons
            var genreElements = document.QuerySelectorAll("[data-testid='genresList'] a span.Button__labelItem");

            if (genreElements.Length == 0)
            {
                genreElements = document.QuerySelectorAll(".BookPageMetadataSection__genreButton a span");
            }

            if (genreElements.Length == 0)
            {
                // Legacy layout - shelf links
                genreElements = document.QuerySelectorAll(".left .bookPageGenreLink");
            }

            if (genreElements.Length == 0)
            {
                genreElements = document.QuerySelectorAll("a[href*='/genres/']");
            }

            var addedGenres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var genreElement in genreElements)
            {
                var genre = CleanText(genreElement.TextContent);
                if (!string.IsNullOrEmpty(genre) &&
                    !addedGenres.Contains(genre) &&
                    !genre.Contains("...more", StringComparison.OrdinalIgnoreCase))
                {
                    metadata.Genres.Add(genre);
                    addedGenres.Add(genre);
                }
            }
        }

        private static void ParseIdentifiers(IDocument document, AudiobookMetadata metadata)
        {
            var detailsText = document.Body?.TextContent ?? "";

            // ISBN-13
            var isbn13Match = Regex.Match(detailsText, @"ISBN13[:\s]+(\d{13})", RegexOptions.IgnoreCase);
            if (isbn13Match.Success)
            {
                metadata.Isbn13 = isbn13Match.Groups[1].Value;
            }
            else
            {
                // Alternative pattern
                isbn13Match = Regex.Match(detailsText, @"\b(97[89]\d{10})\b");
                if (isbn13Match.Success)
                {
                    metadata.Isbn13 = isbn13Match.Groups[1].Value;
                }
            }

            // ISBN-10
            var isbn10Match = Regex.Match(detailsText, @"ISBN[:\s]+(\d{9}[\dXx])", RegexOptions.IgnoreCase);
            if (isbn10Match.Success)
            {
                metadata.Isbn = isbn10Match.Groups[1].Value;
            }

            // ASIN
            var asinMatch = Regex.Match(detailsText, @"ASIN[:\s]+([A-Z0-9]{10})", RegexOptions.IgnoreCase);
            if (asinMatch.Success)
            {
                metadata.Asin = asinMatch.Groups[1].Value;
            }
        }

        private static void ParsePageCount(IDocument document, AudiobookMetadata metadata)
        {
            var pagesElement = document.QuerySelector("[data-testid='pagesFormat']") ??
                              document.QuerySelector("[itemprop='numberOfPages']") ??
                              document.QuerySelector("span[itemprop='numberOfPages']");

            var pagesText = pagesElement?.TextContent ?? document.Body?.TextContent ?? "";

            var pageMatch = PageCountRegex().Match(pagesText);
            if (pageMatch.Success && int.TryParse(pageMatch.Groups[1].Value, out var pages))
            {
                metadata.PageCount = pages;
            }
        }

        private void ApplyBrowserHeaders(HttpRequestMessage request)
        {
            var userAgent = _userAgentRotator.GetNextUserAgent();
            var headers = UserAgentRotator.GetBrowserHeaders(userAgent);

            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Add referer to look more legitimate
            request.Headers.TryAddWithoutValidation("Referer", _settings.BaseUrl);
        }

        private async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
        {
            await _rateLimitSemaphore.WaitAsync(cancellationToken);
            try
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                var requiredDelay = TimeSpan.FromMilliseconds(_settings.RequestDelayMs);

                if (timeSinceLastRequest < requiredDelay)
                {
                    var delay = requiredDelay - timeSinceLastRequest;
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

        private static string CleanText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        private static string? CleanHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return null;

            // Convert <br> to newlines
            var text = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);

            // Remove all other HTML tags
            text = Regex.Replace(text, @"<[^>]+>", "");

            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);

            // Clean up whitespace
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"\n\s*\n\s*\n+", "\n\n");

            return text.Trim();
        }

        private static string? GetHighResolutionImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;

            // Goodreads image URL patterns:
            // Small: https://i.gr-assets.com/images/S/compressed.photo.goodreads.com/books/XXXXXXX._SX50_.jpg
            // Large: https://i.gr-assets.com/images/S/compressed.photo.goodreads.com/books/XXXXXXX.jpg

            // Remove size modifiers to get full resolution
            var highRes = Regex.Replace(imageUrl, @"\._S[XY]\d+_", "");
            highRes = Regex.Replace(highRes, @"\._[A-Z]{2}\d+_", "");

            // Also handle alternative patterns
            highRes = Regex.Replace(highRes, @"\._(.*?)_\.", ".");

            return highRes;
        }

        public void Dispose()
        {
            _rateLimitSemaphore.Dispose();
            _browsingContext.Dispose();
        }
    }
}
