using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Goodreads.Scraper.Models;

namespace Goodreads.Scraper
{
    /// <summary>
    /// Interface for Goodreads web scraping service.
    /// </summary>
    public interface IGoodreadsScraperService
    {
        /// <summary>
        /// Searches Goodreads for books matching the query.
        /// </summary>
        /// <param name="query">Search query (book title, author, etc.).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of search results.</returns>
        Task<IReadOnlyList<GoodreadsSearchResult>> SearchBooksAsync(
            string query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed metadata for a specific book.
        /// </summary>
        /// <param name="bookId">Goodreads book ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Book metadata or null if not found.</returns>
        Task<AudiobookMetadata?> GetBookMetadataAsync(
            string bookId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for a book and returns detailed metadata for the best match.
        /// </summary>
        /// <param name="query">Search query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Book metadata or null if no match found.</returns>
        Task<AudiobookMetadata?> SearchAndGetMetadataAsync(
            string query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads the cover image for a book.
        /// </summary>
        /// <param name="imageUrl">URL of the cover image.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Image data as byte array, or null if download failed.</returns>
        Task<byte[]?> DownloadCoverImageAsync(
            string imageUrl,
            CancellationToken cancellationToken = default);
    }
}
