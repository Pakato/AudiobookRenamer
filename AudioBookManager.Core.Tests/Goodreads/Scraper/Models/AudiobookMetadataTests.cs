using FluentAssertions;
using Goodreads.Scraper.Models;

namespace AudioBookManager.Core.Tests.Goodreads.Scraper.Models;

/// <summary>
/// Unit tests for AudiobookMetadata model.
/// </summary>
public class AudiobookMetadataTests
{
    [Fact]
    public void PrimaryAuthor_WithAuthors_ReturnsFirstAuthor()
    {
        // Arrange
        var metadata = new AudiobookMetadata
        {
            Authors = ["J.R.R. Tolkien", "Christopher Tolkien"]
        };

        // Assert
        metadata.PrimaryAuthor.Should().Be("J.R.R. Tolkien");
    }

    [Fact]
    public void PrimaryAuthor_WithNoAuthors_ReturnsNull()
    {
        // Arrange
        var metadata = new AudiobookMetadata();

        // Assert
        metadata.PrimaryAuthor.Should().BeNull();
    }

    [Fact]
    public void PrimaryGenre_WithGenres_ReturnsFirstGenre()
    {
        // Arrange
        var metadata = new AudiobookMetadata
        {
            Genres = ["Fantasy", "Classics", "Fiction"]
        };

        // Assert
        metadata.PrimaryGenre.Should().Be("Fantasy");
    }

    [Fact]
    public void PrimaryGenre_WithNoGenres_ReturnsNull()
    {
        // Arrange
        var metadata = new AudiobookMetadata();

        // Assert
        metadata.PrimaryGenre.Should().BeNull();
    }

    [Fact]
    public void FormattedSeries_WithSeriesAndNumber_ReturnsFormattedString()
    {
        // Arrange
        var metadata = new AudiobookMetadata
        {
            Series = "The Lord of the Rings",
            SeriesNumber = "1"
        };

        // Assert
        metadata.FormattedSeries.Should().Be("The Lord of the Rings #1");
    }

    [Fact]
    public void FormattedSeries_WithSeriesOnly_ReturnsSeriesName()
    {
        // Arrange
        var metadata = new AudiobookMetadata
        {
            Series = "Standalone Series",
            SeriesNumber = null
        };

        // Assert
        metadata.FormattedSeries.Should().Be("Standalone Series");
    }

    [Fact]
    public void FormattedSeries_WithNoSeries_ReturnsNull()
    {
        // Arrange
        var metadata = new AudiobookMetadata();

        // Assert
        metadata.FormattedSeries.Should().BeNull();
    }

    [Fact]
    public void FormattedSeries_WithDecimalSeriesNumber_FormatsCorrectly()
    {
        // Arrange
        var metadata = new AudiobookMetadata
        {
            Series = "The Witcher",
            SeriesNumber = "0.5"
        };

        // Assert
        metadata.FormattedSeries.Should().Be("The Witcher #0.5");
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var metadata = new AudiobookMetadata();

        // Assert
        metadata.GoodreadsId.Should().Be(string.Empty);
        metadata.Title.Should().Be(string.Empty);
        metadata.Authors.Should().BeEmpty();
        metadata.Narrators.Should().BeEmpty();
        metadata.Genres.Should().BeEmpty();
        metadata.ScrapedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var coverData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var metadata = new AudiobookMetadata
        {
            GoodreadsId = "12345",
            Title = "Test Book",
            Series = "Test Series",
            SeriesNumber = "1",
            Authors = ["Author One", "Author Two"],
            Narrators = ["Narrator One"],
            Year = 2023,
            OriginalYear = 2020,
            Publisher = "Test Publisher",
            Description = "Test description",
            Genres = ["Fantasy", "Adventure"],
            Isbn = "1234567890",
            Isbn13 = "9781234567890",
            Asin = "B01234567X",
            Rating = 4.5m,
            RatingsCount = 1000,
            CoverImageUrl = "https://example.com/cover.jpg",
            SmallCoverImageUrl = "https://example.com/cover_small.jpg",
            CoverImageData = coverData,
            PageCount = 350,
            Language = "eng",
            Duration = TimeSpan.FromHours(12),
            GoodreadsUrl = "https://www.goodreads.com/book/show/12345",
            ScrapedAt = now
        };

        // Assert
        metadata.GoodreadsId.Should().Be("12345");
        metadata.Title.Should().Be("Test Book");
        metadata.Series.Should().Be("Test Series");
        metadata.SeriesNumber.Should().Be("1");
        metadata.Authors.Should().HaveCount(2);
        metadata.Narrators.Should().HaveCount(1);
        metadata.Year.Should().Be(2023);
        metadata.OriginalYear.Should().Be(2020);
        metadata.Publisher.Should().Be("Test Publisher");
        metadata.Description.Should().Be("Test description");
        metadata.Genres.Should().HaveCount(2);
        metadata.Isbn.Should().Be("1234567890");
        metadata.Isbn13.Should().Be("9781234567890");
        metadata.Asin.Should().Be("B01234567X");
        metadata.Rating.Should().Be(4.5m);
        metadata.RatingsCount.Should().Be(1000);
        metadata.CoverImageUrl.Should().Be("https://example.com/cover.jpg");
        metadata.SmallCoverImageUrl.Should().Be("https://example.com/cover_small.jpg");
        metadata.CoverImageData.Should().BeEquivalentTo(coverData);
        metadata.PageCount.Should().Be(350);
        metadata.Language.Should().Be("eng");
        metadata.Duration.Should().Be(TimeSpan.FromHours(12));
        metadata.GoodreadsUrl.Should().Be("https://www.goodreads.com/book/show/12345");
        metadata.ScrapedAt.Should().Be(now);
    }
}
