using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Goodreads.Helpers;
using Goodreads.Http;
using Goodreads.Models.Request;
using Goodreads.Models.Response;
using RestSharp;

namespace Goodreads.Clients
{
    /// <summary>
    /// The client class for the Book endpoint of the Goodreads API.
    /// </summary>
    internal sealed class BooksEndpoint : Endpoint, IOAuthBooksEndpoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BooksEndpoint"/> class.
        /// </summary>
        /// <param name="connection">A RestClient connection to the Goodreads API.</param>
        public BooksEndpoint(IConnection connection) : base(connection)
        {
        }

        /// <summary>
        /// Get book information by ISBN.
        /// </summary>
        /// <param name="isbn">The ISBN of the desired book.</param>
        /// <returns>An async task returning the desired book information.</returns>
        public async Task<Book> GetByIsbn(string isbn)
        {
            var parameters = new List<Parameter>
            {
                Parameter.CreateParameter("isbn", isbn, ParameterType.UrlSegment)
                };

            return await Connection.ExecuteRequest<Book>("book/isbn/{isbn}.xml", parameters, null, "book")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Get book information by Goodreads book id.
        /// </summary>
        /// <param name="bookId">The Goodreads book id.</param>
        /// <returns>Information about the Goodreads book, null if not found.</returns>
        public async Task<Book> GetByBookId(long bookId)
        {
            var parameters = new List<Parameter>
            {
                Parameter.CreateParameter("bookId", bookId, ParameterType.UrlSegment)
            };

            return await Connection.ExecuteRequest<Book>("book/show/{bookId}.xml", parameters, null, "book")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Get book information by book title.
        /// Include an author name for increased accuracy.
        /// </summary>
        /// <param name="title">The book title to find.</param>
        /// <param name="author">The author of the book, optional but include it for increased accuracy.</param>
        /// <param name="rating">Show only reviews with a particular rating (optional)</param>
        /// <returns>Information about the Goodreads book, null if not found.</returns>
        public async Task<Book> GetByTitle(string title, string author, int? rating)
        {
            var parameters = new List<Parameter>
            {
                Parameter.CreateParameter("title", title, ParameterType.QueryString)
            };

            if (!string.IsNullOrEmpty(author))
            {
                parameters.Add(Parameter.CreateParameter("author", author, ParameterType.QueryString));
            }

            if (rating.HasValue)
            {
                parameters.Add(Parameter.CreateParameter("rating", rating.Value, ParameterType.QueryString));
            }

            return await Connection.ExecuteRequest<Book>("book/title.xml", parameters, null, "book")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a paginated list of books written by the given author.
        /// </summary>
        /// <param name="authorId">The Goodreads author id.</param>
        /// <param name="page">The desired page from the paginated list of books.</param>
        /// <returns>A paginated list of books written by the author.</returns>
        public async Task<PaginatedList<Book>> GetListByAuthorId(long authorId, int page)
        {
            var parameters = new List<Parameter>
            {
                Parameter.CreateParameter("authorId", authorId, ParameterType.UrlSegment),
                Parameter.CreateParameter("page", page, ParameterType.QueryString)
            };

            return await Connection.ExecuteRequest<PaginatedList<Book>>("author/list/{authorId}", parameters, null, "author/books")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Search Goodreads for books (returned as <see cref="Work"/> objects).
        /// </summary>
        /// <param name="searchTerm">The search term to search Goodreads with.</param>
        /// <param name="page">The current page of the paginated list.</param>
        /// <param name="searchField">The book fields to apply the search term against.</param>
        /// <returns>A paginated list of <see cref="Work"/> object matching the given search criteria.</returns>
        public async Task<PaginatedList<Work>> Search(string searchTerm, int page, BookSearchField searchField)
        {
            var parameters = new List<Parameter>
            {
                Parameter.CreateParameter("q", searchTerm, ParameterType.QueryString),
                Parameter.CreateParameter("page", page, ParameterType.QueryString),
                Parameter.CreateParameter(
                    EnumHelpers.QueryParameterKey<BookSearchField>(),
                    EnumHelpers.QueryParameterValue(searchField),
                    ParameterType.QueryString)
            };

            return await Connection.ExecuteRequest<PaginatedList<Work>>("search", parameters, null, "search").ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a single Goodreads book id for the given ISBN10 or ISBN13.
        /// </summary>
        /// <param name="isbn">The ISBN number to fetch a book it for. Can be ISBN10 or ISBN13.</param>
        /// <returns>A Goodreads book id if found, null otherwise.</returns>
        public async Task<long?> GetBookIdForIsbn(string isbn)
        {
            var bookIds = await this.GetBookIdsForIsbns(new List<string> { isbn });
            return bookIds?.FirstOrDefault();
        }

        /// <summary>
        /// Converts a list of ISBNs (ISBN10 or ISBN13) to Goodreads book ids.
        /// The ordering and size of the list is kept consistent with missing
        /// ISBNs substituted with null.
        /// </summary>
        /// <param name="isbns">The list of ISBNs to convert.</param>
        /// <returns>A list of Goodreads book ids (with null elements if an ISBN wasn't found).</returns>
        public async Task<IReadOnlyList<long?>> GetBookIdsForIsbns(IReadOnlyList<string> isbns)
        {
            var parameters = new List<Parameter>
            {
                Parameter.CreateParameter("isbn", string.Join(",", isbns), ParameterType.QueryString)
            };

            // This endpoint doesn't actually return XML. But instead returns a comma delimited list of book ids.
            // If an ISBN isn't found, an empty string is returned at that index.
            var response = await Connection.ExecuteRaw("book/isbn_to_id", parameters).ConfigureAwait(false);
            if (response != null && (int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
            {
                var content = response.Content;
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var responseIds = content.Split(',');
                    if (responseIds.Any())
                    {
                        var bookIds = new List<long?>();
                        foreach (var responseId in responseIds)
                        {
                            if (!string.IsNullOrEmpty(responseId))
                            {
                                bookIds.Add(long.Parse(responseId));
                            }
                            else
                            {
                                bookIds.Add(null);
                            }
                        }

                        return bookIds;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Converts a list of Goodreads book ids to work ids.
        /// The ordering and size of the list is kept consistent with missing
        /// book ids substituted with null.
        /// </summary>
        /// <param name="bookIds">The list of Goodreads book ids to convert.</param>
        /// <returns>A list of work ids corresponding to the given book ids.</returns>
        public async Task<IReadOnlyList<long?>> GetWorkIdsForBookIds(IReadOnlyList<long> bookIds)
        {
            var parameters = new List<Parameter>
            {
                Parameter.CreateParameter("bookIds", string.Join(",", bookIds), ParameterType.UrlSegment)
            };

            // This response is simple enough that we just parse it here without creating another model
            var response = await Connection.ExecuteRaw("book/id_to_work_id/{bookIds}", parameters).ConfigureAwait(false);
            if (response != null && (int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
            {
                var content = response.Content;
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var workIds = new List<long?>();
                    var document = XDocument.Parse(content);
                    var items = document.XPathSelectElements("GoodreadsResponse/work-ids/item");
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Value))
                        {
                            workIds.Add(long.Parse(item.Value));
                        }
                        else
                        {
                            workIds.Add(null);
                        }
                    }

                    return workIds;
                }
            }

            return null;
        }

        /// <summary>
        /// Get review statistics for a list of books by ISBN10 or ISBN13.
        /// </summary>
        /// <param name="isbns">A list of ISBN10 or ISBN13s to retrieve stats for.</param>
        /// <returns>A list of review stats for the given ISBNs.</returns>
        public async Task<IReadOnlyList<ReviewStats>> GetReviewStatsForIsbns(IReadOnlyList<string> isbns)
        {
            var parameters = new List<Parameter>
            {
                Parameter.CreateParameter("isbns", string.Join(",", isbns), ParameterType.QueryString)
            };

            // This endpoint only supports JSON for some reason...
            var response = await Connection.ExecuteJsonRequest<ReviewStatsContainer>("book/review_counts.json", parameters).ConfigureAwait(false);
            return response?.Books;
        }
    }
}
