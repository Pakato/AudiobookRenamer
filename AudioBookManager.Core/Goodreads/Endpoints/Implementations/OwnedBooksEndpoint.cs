using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Goodreads.Http;
using Goodreads.Models.Response;
using RestSharp;

namespace Goodreads.Clients
{
    internal sealed class OwnedBooksEndpoint : IOAuthOwnedBooksEndpoint
    {
        private readonly IConnection Connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="OwnedBooksEndpoint"/> class.
        /// </summary>
        /// <param name="connection">A RestClient connection to the Goodreads API.</param>
        public OwnedBooksEndpoint(IConnection connection)
        {
            Connection = connection;
        }

        public async Task<PaginatedList<OwnedBook>> GetOwnedBooks(long userId, int page)
        {
            var endpoint = @"owned_books/user";

            var parameters = new List<Parameter>
            {
                Parameter.CreateParameter("id", userId, ParameterType.QueryString),
                Parameter.CreateParameter("page", page, ParameterType.QueryString)
            };

            return await Connection.ExecuteRequest<PaginatedList<OwnedBook>>(endpoint, parameters, null, "owned_books").ConfigureAwait(false);
        }

        /// <summary>
        /// Get an owned book, including the current owner's user id.
        /// </summary>
        /// <param name="ownedBookId">A desire owned book id.</param>
        /// <returns>An owned book information.</returns>
        public async Task<OwnedBook> GetOwnedBookInfo(long ownedBookId)
        {
            var endpoint = $"owned_books/show/{ownedBookId}";

            return await Connection.ExecuteRequest<OwnedBook>(endpoint, new List<Parameter>(), null, "owned_book/owned_book").ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a book to user's list of owned books.
        /// </summary>
        /// <param name="bookId">Id of the book.</param>
        /// <param name="code">An one of 10 (brand new), 20 (like new), 30 (very good), 40 (good), 50 (acceptable), 60 (poor).</param>
        /// <param name="description">A description of book's.</param>
        /// <param name="purchaseDate">A date when book was purchased.</param>
        /// <param name="purchaseLocation">A location where this book was purchased.</param>
        /// <param name="bcid">BookCrossing id (BCID).</param>
        /// <returns>>An owned book object.</returns>
        public async Task<OwnedBookSummary> AddOwnedBook(
            long bookId,
            int code,
            string description,
            DateTime? purchaseDate,
            string purchaseLocation,
            int? bcid)
        {
            var endpoint = @"owned_books";

            var parameters = new List<Parameter>
            {
                Parameter.CreateParameter("owned_book[book_id]", bookId, ParameterType.QueryString),
                Parameter.CreateParameter("owned_book[condition_code]", code, ParameterType.QueryString)
            };

            if (!string.IsNullOrEmpty(description))
            {
                parameters.Add(Parameter.CreateParameter("owned_book[condition_description]", description, ParameterType.QueryString));
            }

            if (purchaseDate.HasValue)
            {
                parameters.Add(Parameter.CreateParameter("owned_book[original_purchase_date]", purchaseDate.Value.ToString("yyyy/MM/dd"), ParameterType.QueryString));
            }

            if (!string.IsNullOrEmpty(purchaseLocation))
            {
                parameters.Add(Parameter.CreateParameter("owned_book[original_purchase_location]", purchaseLocation, ParameterType.QueryString));
            }

            if (bcid.HasValue)
            {
                parameters.Add(Parameter.CreateParameter("owned_book[unique_code]", bcid, ParameterType.QueryString));
            }

            return await Connection.ExecuteRequest<OwnedBookSummary>(endpoint, parameters, null, "owned-book", Method.Post).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a book a user owns.
        /// </summary>
        /// <param name="ownedBookId">Id of the owned book.</param>
        /// <returns>True if deleting successed, otherwise false.</returns>
        public async Task<bool> DeleteOwnedBook(long ownedBookId)
        {
            var endpoint = $"owned_books/destroy/{ownedBookId}";
            var response = await Connection.ExecuteRaw(endpoint, null, Method.Post).ConfigureAwait(false);

            return response.StatusCode == HttpStatusCode.NoContent;
        }
    }
}
