using System;
using System.Net;

namespace SendAfrica
{
    /// <summary>
    /// Thrown when the SendAfrica API returns a non-success (non-2xx) HTTP response.
    /// </summary>
    public class SendAfricaApiException : Exception
    {
        /// <summary>The HTTP status code returned by the API.</summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>The raw response body returned by the API, if any.</summary>
        public string? ResponseBody { get; }

        /// <summary>Creates a new <see cref="SendAfricaApiException"/>.</summary>
        public SendAfricaApiException(HttpStatusCode statusCode, string? responseBody)
            : base($"SendAfrica API request failed with status {(int)statusCode} ({statusCode}). Response: {responseBody}")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
