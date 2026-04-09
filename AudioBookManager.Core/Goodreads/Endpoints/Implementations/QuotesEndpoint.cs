using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Goodreads.Http;
using RestSharp;

namespace Goodreads.Clients
{
    /// <summary>
    /// The client class for the Quotes endpoint of the Goodreads API.
    /// </summary>
    internal sealed class QuotesEndpoint : Endpoint, IOAuthQuotesEndpoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuotesEndpoint"/> class.
        /// </summary>
        /// <param name="connection">A RestClient connection to the Goodreads API.</param>
        public QuotesEndpoint(IConnection connection)
            : base(connection)
        {
        }

        /// <summary>
        /// Add a quote.
        /// </summary>
        /// <param name="authorId">The Goodreads author id.</param>
        /// <param name="authorName">Name of the quote author.</param>
        /// <param name="quote">The quote.</param>
        /// <param name="bookId">Id of the book from which the quote was taken.</param>
        /// <param name="isbn">ISBN of the book from which the quote was taken.
        /// This will not override the book_id if it was provided.</param>
        /// <returns>True if adding succeeded, false otherwise.</returns>
        public async Task<bool> Add(
            long authorId,
            string authorName,
            string quote,
            long? bookId,
            string isbn)
        {
            var parameters = new List<Parameter>
            {
                Parameter.CreateParameter("quote[author_name]", authorName, ParameterType.QueryString),
                Parameter.CreateParameter("quote[author_id]", authorId, ParameterType.QueryString),
                Parameter.CreateParameter("quote[body]", quote, ParameterType.QueryString),
            };

            if (!bookId.HasValue && string.IsNullOrWhiteSpace(isbn))
            {
                throw new ArgumentException("Should define either bookId or isbn parameter.");
            }

            if (bookId.HasValue)
            {
                parameters.Add(Parameter.CreateParameter("quote[book_id]", bookId.Value, ParameterType.QueryString));
            }

            if (!string.IsNullOrWhiteSpace(isbn))
            {
                parameters.Add(Parameter.CreateParameter("isbn", isbn, ParameterType.QueryString));
            }

            var response = await Connection.ExecuteRaw("quotes", parameters, Method.Post).ConfigureAwait(false);

            return response.StatusCode == HttpStatusCode.Created;
        }
    }
}
