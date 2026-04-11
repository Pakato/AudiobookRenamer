using System;
using System.Collections.Generic;

namespace Goodreads.Scraper.Models
{
    /// <summary>
    /// Represents metadata extracted from Goodreads for an audiobook,
    /// suitable for populating ID3 tags.
    /// </summary>
    public sealed class AudiobookMetadata
    {
        /// <summary>
        /// The Goodreads book ID.
        /// </summary>
        public string GoodreadsId { get; set; } = string.Empty;

        /// <summary>
        /// The book title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The series name, if part of a series.
        /// </summary>
        public string? Series { get; set; }

        /// <summary>
        /// The book's position in the series (e.g., "1", "2.5").
        /// </summary>
        public string? SeriesNumber { get; set; }

        /// <summary>
        /// List of authors.
        /// </summary>
        public List<string> Authors { get; set; } = [];

        /// <summary>
        /// List of narrators (for audiobooks).
        /// </summary>
        public List<string> Narrators { get; set; } = [];

        /// <summary>
        /// Publication year.
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// Original publication year.
        /// </summary>
        public int? OriginalYear { get; set; }

        /// <summary>
        /// Publisher name.
        /// </summary>
        public string? Publisher { get; set; }

        /// <summary>
        /// Book description/summary.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// List of genres/shelves from Goodreads.
        /// </summary>
        public List<string> Genres { get; set; } = [];

        /// <summary>
        /// ISBN-10 identifier.
        /// </summary>
        public string? Isbn { get; set; }

        /// <summary>
        /// ISBN-13 identifier.
        /// </summary>
        public string? Isbn13 { get; set; }

        /// <summary>
        /// ASIN (Amazon Standard Identification Number).
        /// </summary>
        public string? Asin { get; set; }

        /// <summary>
        /// Average rating on Goodreads (0-5 scale).
        /// </summary>
        public decimal? Rating { get; set; }

        /// <summary>
        /// Number of ratings on Goodreads.
        /// </summary>
        public int? RatingsCount { get; set; }

        /// <summary>
        /// URL to the book cover image (high resolution).
        /// </summary>
        public string? CoverImageUrl { get; set; }

        /// <summary>
        /// URL to the book cover image (small/thumbnail).
        /// </summary>
        public string? SmallCoverImageUrl { get; set; }

        /// <summary>
        /// Raw cover image bytes (downloaded separately).
        /// </summary>
        public byte[]? CoverImageData { get; set; }

        /// <summary>
        /// Number of pages in the book.
        /// </summary>
        public int? PageCount { get; set; }

        /// <summary>
        /// Language of the book (ISO code).
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Duration of the audiobook (if available).
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// URL to the book page on Goodreads.
        /// </summary>
        public string? GoodreadsUrl { get; set; }

        /// <summary>
        /// Timestamp when metadata was scraped.
        /// </summary>
        public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the primary author (first in the list).
        /// </summary>
        public string? PrimaryAuthor => Authors.Count > 0 ? Authors[0] : null;

        /// <summary>
        /// Gets the primary genre (first in the list).
        /// </summary>
        public string? PrimaryGenre => Genres.Count > 0 ? Genres[0] : null;

        /// <summary>
        /// Gets the formatted series info (e.g., "Series Name #1").
        /// </summary>
        public string? FormattedSeries =>
            !string.IsNullOrEmpty(Series)
                ? !string.IsNullOrEmpty(SeriesNumber)
                    ? $"{Series} #{SeriesNumber}"
                    : Series
                : null;
    }
}
