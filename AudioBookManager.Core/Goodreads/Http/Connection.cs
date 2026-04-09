using Goodreads.Extensions;
using Goodreads.Models;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Goodreads.Http
{
    internal class Connection : IConnection
    {
        private const string GoodreadsUrl = @"https://www.goodreads.com/";
        private const string GoodreadsUserAgent = @"goodreads-dotnet";
        private readonly IRestClient _client;

        /// <summary>
        /// Credentials for the Goodreads API.
        /// </summary>
        public ApiCredentials Credentials { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="credentials">Credentials for use with the Goodreads API.</param>
        public Connection(ApiCredentials credentials)
        {
            _client = CreateClient(credentials);
            Credentials = credentials;
        }

        public async Task<RestResponse> ExecuteRaw(
            string endpoint,
            IEnumerable<Parameter> parameters,
            Method method = Method.Get)
        {
            var request = BuildRequest(endpoint, parameters);
            request.Method = method;
            return await _client.ExecuteTaskRaw(request).ConfigureAwait(false) as RestResponse;
        }

        public async Task<T> ExecuteRequest<T>(
            string endpoint,
            IEnumerable<Parameter> parameters,
            object data = null,
            string expectedRoot = null,
            Method method = Method.Get)
            where T : ApiResponse, new()
        {
            var request = BuildRequest(endpoint, parameters);
            request.RootElement = expectedRoot;
            request.Method = method;

            if (data != null && method != Method.Get)
            {
                request.RequestFormat = DataFormat.Xml;
                request.AddBody(data);
            }

            return await _client.ExecuteTask<T>(request).ConfigureAwait(false);
        }

        public async Task<T> ExecuteJsonRequest<T>(string endpoint, IEnumerable<Parameter> parameters)
        {
            var request = BuildRequest(endpoint, parameters);
            var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
            // Deserialize the response content to T
            // Replace with your actual deserialization logic if available
            // For now, using default if deserialization is not available
            return string.IsNullOrEmpty(response.Content) ? default : System.Text.Json.JsonSerializer.Deserialize<T>(response.Content);
        }

        public async Task<OAuthAccessToken> GetAccessToken(OAuthRequestToken requestToken)
        {
            var options = new RestClientOptions(GoodreadsUrl)
            {
                UserAgent = GoodreadsUserAgent,
                Authenticator = OAuth1Authenticator.ForAccessToken(
                    Credentials.ApiKey,
                    Credentials.ApiSecret,
                    requestToken.Token,
                    requestToken.Secret)
            };
            using var client = new RestClient(options);

            var request = new RestRequest("oauth/access_token", Method.Post);
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);

            var queryString = HttpUtility.ParseQueryString(response.Content);

            var oAuthToken = queryString["oauth_token"];
            var oAuthTokenSecret = queryString["oauth_token_secret"];

            return new OAuthAccessToken(oAuthToken, oAuthTokenSecret);
        }

        public async Task<OAuthRequestToken> GetRequestToken(string callbackUrl)
        {
            var options = new RestClientOptions(GoodreadsUrl)
            {
                UserAgent = GoodreadsUserAgent,
                Authenticator = OAuth1Authenticator.ForRequestToken(Credentials.ApiKey, Credentials.ApiSecret)
            };
            using var client = new RestClient(options);

            var request = new RestRequest("oauth/request_token", method: Method.Get);
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);

            var queryString = HttpUtility.ParseQueryString(response.Content);

            var oAuthToken = queryString["oauth_token"];
            var oAuthTokenSecret = queryString["oauth_token_secret"];
            var authorizeUrl = BuildAuthorizeUrl(oAuthToken, callbackUrl);

            return new OAuthRequestToken(oAuthToken, oAuthTokenSecret, authorizeUrl);
        }

        private string BuildAuthorizeUrl(string oauthToken, string callbackUrl)
        {
            var request = new RestRequest("oauth/authorize");
            request.AddParameter("oauth_token", oauthToken);

            if (!string.IsNullOrEmpty(callbackUrl))
            {
                request.AddParameter("oauth_callback", callbackUrl);
            }

            return _client.BuildUri(request).ToString();
        }

        private static RestRequest BuildRequest(string endpoint, IEnumerable<Parameter> parameters)
        {
            var request = new RestRequest(endpoint);

            foreach (var parameter in parameters ?? Enumerable.Empty<Parameter>())
            {
                request.AddParameter(parameter);
            }

            return request;
        }

        private static IRestClient CreateClient(ApiCredentials credentials)
        {
            var options = new RestClientOptions(GoodreadsUrl)
            {
                UserAgent = GoodreadsUserAgent
            };

            // If OAuth tokens are present, set the authenticator in the options
            if (!string.IsNullOrEmpty(credentials.OAuthToken) && !string.IsNullOrEmpty(credentials.OAuthTokenSecret))
            {
                options.Authenticator =
                    OAuth1Authenticator.ForProtectedResource(
                        credentials.ApiKey,
                        credentials.ApiSecret,
                        credentials.OAuthToken,
                        credentials.OAuthTokenSecret);
            }

            var client = new RestClient(options);

            client.AddDefaultParameter("key", credentials.ApiKey, ParameterType.QueryString);
            client.AddDefaultParameter("format", "xml", ParameterType.QueryString);

            return client;
        }
    }
}
