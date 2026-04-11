using System.Net;
using FluentAssertions;
using Goodreads.Scraper;
using Goodreads.Scraper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;

namespace AudioBookManager.Core.Tests.Goodreads.Scraper;

/// <summary>
/// Unit tests for GoodreadsScraperService.
/// </summary>
public class GoodreadsScraperServiceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly Mock<ILogger<GoodreadsScraperService>> _loggerMock;
    private readonly IOptions<GoodreadsScraperSettings> _settings;
    private readonly GoodreadsScraperService _sut;

    public GoodreadsScraperServiceTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _loggerMock = new Mock<ILogger<GoodreadsScraperService>>();
        _settings = Options.Create(new GoodreadsScraperSettings
        {
            BaseUrl = "https://www.goodreads.com",
            RequestDelayMs = 0, // No delay for tests
            MaxRetries = 1,
            TimeoutSeconds = 30,
            MaxSearchResults = 10
        });

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_settings.Value.BaseUrl);

        _sut = new GoodreadsScraperService(httpClient, _settings, _loggerMock.Object);
    }

    public void Dispose()
    {
        _sut.Dispose();
        _mockHttp.Dispose();
        GC.SuppressFinalize(this);
    }

    #region SearchBooksAsync Tests

    [Fact]
    public async Task SearchBooksAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var searchHtml = GetSearchResultsHtml();
        _mockHttp.When("https://www.goodreads.com/search*")
            .Respond("text/html", searchHtml);

        // Act
        var results = await _sut.SearchBooksAsync("The Hobbit");

        // Assert
        results.Should().NotBeEmpty();
        results[0].Title.Should().NotBeNullOrEmpty();
        results[0].BookId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SearchBooksAsync_WithEmptyQuery_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SearchBooksAsync(""));
    }

    [Fact]
    public async Task SearchBooksAsync_WithNullQuery_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.SearchBooksAsync(null!));
    }

    [Fact]
    public async Task SearchBooksAsync_WhenNoResults_ReturnsEmptyList()
    {
        // Arrange
        var emptyHtml = "<html><body><table class='tableList'></table></body></html>";
        _mockHttp.When("https://www.goodreads.com/search*")
            .Respond("text/html", emptyHtml);

        // Act
        var results = await _sut.SearchBooksAsync("xyznonexistentbook123");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchBooksAsync_ParsesAuthorsCorrectly()
    {
        // Arrange
        var searchHtml = GetSearchResultsHtml();
        _mockHttp.When("https://www.goodreads.com/search*")
            .Respond("text/html", searchHtml);

        // Act
        var results = await _sut.SearchBooksAsync("The Hobbit");

        // Assert
        results.Should().NotBeEmpty();
        results[0].Authors.Should().NotBeEmpty();
        results[0].Authors.Should().Contain("J.R.R. Tolkien");
    }

    #endregion

    #region GetBookMetadataAsync Tests

    [Fact]
    public async Task GetBookMetadataAsync_WithValidId_ReturnsMetadata()
    {
        // Arrange
        var bookHtml = GetBookPageHtml();
        _mockHttp.When("https://www.goodreads.com/book/show/12345")
            .Respond("text/html", bookHtml);

        // Act
        var metadata = await _sut.GetBookMetadataAsync("12345");

        // Assert
        metadata.Should().NotBeNull();
        metadata!.GoodreadsId.Should().Be("12345");
        metadata.Title.Should().Be("The Hobbit");
    }

    [Fact]
    public async Task GetBookMetadataAsync_WithEmptyId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetBookMetadataAsync(""));
    }

    [Fact]
    public async Task GetBookMetadataAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _mockHttp.When("https://www.goodreads.com/book/show/99999")
            .Respond(HttpStatusCode.NotFound);

        // Act
        var metadata = await _sut.GetBookMetadataAsync("99999");

        // Assert
        metadata.Should().BeNull();
    }

    [Fact]
    public async Task GetBookMetadataAsync_ParsesAllFieldsCorrectly()
    {
        // Arrange
        var bookHtml = GetBookPageHtml();
        _mockHttp.When("https://www.goodreads.com/book/show/12345")
            .Respond("text/html", bookHtml);

        // Act
        var metadata = await _sut.GetBookMetadataAsync("12345");

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Title.Should().Be("The Hobbit");
        metadata.Authors.Should().Contain("J.R.R. Tolkien");
        metadata.Year.Should().Be(1937);
        metadata.Description.Should().Contain("Bilbo Baggins");
        metadata.Genres.Should().Contain("Fantasy");
        metadata.Rating.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetBookMetadataAsync_ParsesSeriesInfoCorrectly()
    {
        // Arrange
        var bookHtml = GetBookPageWithSeriesHtml();
        _mockHttp.When("https://www.goodreads.com/book/show/54321")
            .Respond("text/html", bookHtml);

        // Act
        var metadata = await _sut.GetBookMetadataAsync("54321");

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Series.Should().Be("Middle-earth Universe");
        metadata.SeriesNumber.Should().Be("1");
        metadata.FormattedSeries.Should().Be("Middle-earth Universe #1");
    }

    #endregion

    #region SearchAndGetMetadataAsync Tests

    [Fact]
    public async Task SearchAndGetMetadataAsync_ReturnsFirstMatchMetadata()
    {
        // Arrange
        var searchHtml = GetSearchResultsHtml();
        var bookHtml = GetBookPageHtml();

        _mockHttp.When("https://www.goodreads.com/search*")
            .Respond("text/html", searchHtml);
        _mockHttp.When("https://www.goodreads.com/book/show/12345")
            .Respond("text/html", bookHtml);

        // Act
        var metadata = await _sut.SearchAndGetMetadataAsync("The Hobbit");

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Title.Should().Be("The Hobbit");
    }

    [Fact]
    public async Task SearchAndGetMetadataAsync_WhenNoResults_ReturnsNull()
    {
        // Arrange
        var emptyHtml = "<html><body><table class='tableList'></table></body></html>";
        _mockHttp.When("https://www.goodreads.com/search*")
            .Respond("text/html", emptyHtml);

        // Act
        var metadata = await _sut.SearchAndGetMetadataAsync("xyznonexistent123");

        // Assert
        metadata.Should().BeNull();
    }

    #endregion

    #region DownloadCoverImageAsync Tests

    [Fact]
    public async Task DownloadCoverImageAsync_WithValidUrl_ReturnsImageData()
    {
        // Arrange
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header
        _mockHttp.When("https://images.goodreads.com/books/cover.jpg")
            .Respond("image/jpeg", new MemoryStream(imageData));

        // Act
        var result = await _sut.DownloadCoverImageAsync("https://images.goodreads.com/books/cover.jpg");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(imageData);
    }

    [Fact]
    public async Task DownloadCoverImageAsync_WithNullUrl_ReturnsNull()
    {
        // Act
        var result = await _sut.DownloadCoverImageAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadCoverImageAsync_WithEmptyUrl_ReturnsNull()
    {
        // Act
        var result = await _sut.DownloadCoverImageAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadCoverImageAsync_WhenRequestFails_ReturnsNull()
    {
        // Arrange
        _mockHttp.When("https://images.goodreads.com/books/notfound.jpg")
            .Respond(HttpStatusCode.NotFound);

        // Act
        var result = await _sut.DownloadCoverImageAsync("https://images.goodreads.com/books/notfound.jpg");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task RateLimiting_EnforcesDelayBetweenRequests()
    {
        // Arrange
        var settings = Options.Create(new GoodreadsScraperSettings
        {
            BaseUrl = "https://www.goodreads.com",
            RequestDelayMs = 100, // 100ms delay
            MaxRetries = 1
        });

        using var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://www.goodreads.com/search*")
            .Respond("text/html", "<html><body><table class='tableList'></table></body></html>");

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(settings.Value.BaseUrl);

        using var service = new GoodreadsScraperService(httpClient, settings, _loggerMock.Object);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await service.SearchBooksAsync("test1");
        await service.SearchBooksAsync("test2");
        stopwatch.Stop();

        // Assert - second request should be delayed
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(90); // Allow some tolerance
    }

    #endregion

    #region HTML Test Data Helpers

    private static string GetSearchResultsHtml()
    {
        return """
            <html>
            <body>
                <table class="tableList">
                    <tr itemtype="http://schema.org/Book">
                        <td>
                            <a class="bookTitle" href="/book/show/12345">
                                <span itemprop="name">The Hobbit</span>
                            </a>
                            <span itemprop="author">
                                <a class="authorName">
                                    <span itemprop="name">J.R.R. Tolkien</span>
                                </a>
                            </span>
                            <img class="bookCover" src="https://images.goodreads.com/books/hobbit.jpg" />
                            <span class="minirating">4.28 avg rating — 3,500,000 ratings</span>
                            <span class="greyText smallText">published 1937</span>
                        </td>
                    </tr>
                    <tr itemtype="http://schema.org/Book">
                        <td>
                            <a class="bookTitle" href="/book/show/67890">
                                <span itemprop="name">The Lord of the Rings</span>
                            </a>
                            <span itemprop="author">
                                <a class="authorName">
                                    <span itemprop="name">J.R.R. Tolkien</span>
                                </a>
                            </span>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }

    private static string GetBookPageHtml()
    {
        return """
            <html>
            <body>
                <h1 data-testid="bookTitle">The Hobbit</h1>

                <div data-testid="authorsList">
                    <a href="/author/show/656983">
                        <span itemprop="name">J.R.R. Tolkien</span>
                    </a>
                </div>

                <div data-testid="RatingStatistics">
                    <div class="RatingStatistics__rating">4.28</div>
                </div>
                <span data-testid="ratingsCount">3,547,132 ratings</span>

                <div data-testid="description">
                    <div class="Formatted">
                        Bilbo Baggins is a hobbit who enjoys a comfortable, unambitious life.
                    </div>
                </div>

                <div data-testid="publicationInfo">
                    Published September 21st 1937 by Houghton Mifflin
                </div>

                <div data-testid="genresList">
                    <a href="/genres/fantasy"><span class="Button__labelItem">Fantasy</span></a>
                    <a href="/genres/classics"><span class="Button__labelItem">Classics</span></a>
                    <a href="/genres/fiction"><span class="Button__labelItem">Fiction</span></a>
                </div>

                <img class="ResponsiveImage" src="https://images.goodreads.com/books/hobbit_large.jpg" />

                <div data-testid="pagesFormat">310 pages, Paperback</div>

                <div id="details">
                    ISBN13: 9780547928227
                    ASIN: B007978NPG
                </div>
            </body>
            </html>
            """;
    }

    private static string GetBookPageWithSeriesHtml()
    {
        return """
            <html>
            <body>
                <h1 data-testid="bookTitle">The Hobbit</h1>

                <h3 class="Text__title3">
                    <a href="/series/12345">Middle-earth Universe #1</a>
                </h3>

                <div data-testid="authorsList">
                    <a href="/author/show/656983">
                        <span itemprop="name">J.R.R. Tolkien</span>
                    </a>
                </div>

                <div data-testid="RatingStatistics">
                    <div class="RatingStatistics__rating">4.28</div>
                </div>

                <div data-testid="description">
                    <div class="Formatted">A great adventure story.</div>
                </div>

                <div data-testid="publicationInfo">Published 1937</div>

                <div data-testid="genresList">
                    <a href="/genres/fantasy"><span class="Button__labelItem">Fantasy</span></a>
                </div>
            </body>
            </html>
            """;
    }

    #endregion
}
