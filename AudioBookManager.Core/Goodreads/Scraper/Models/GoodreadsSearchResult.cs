using System.Collections.Generic;

namespace Goodreads.Scraper.Models
{
    /// <summary>
    /// Represents a search result item from Goodreads search.
    /// </summary>
    public sealed class GoodreadsSearchResult
    {
        /// <summary>
        /// The Goodreads book ID.
        /// </summary>
        public string BookId { get; set; } = string.Empty;

        /// <summary>
        /// The book title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The authors of the book.
        /// </summary>
        public List<string> Authors { get; set; } = [];

        /// <summary>
        /// Average rating.
        /// </summary>
        public decimal? Rating { get; set; }

        /// <summary>
        /// Number of ratings.
        /// </summary>
        public int? RatingsCount { get; set; }

        /// <summary>
        /// Publication year.
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// URL to the book cover thumbnail.
        /// </summary>
        public string? CoverImageUrl { get; set; }

        /// <summary>
        /// Direct URL to the book page.
        /// </summary>
        public string? BookUrl { get; set; }
    }
}
