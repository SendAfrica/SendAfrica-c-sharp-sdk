using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SendAfrica
{
    /// <summary>
    /// Thrown when the SendAfrica API returns a non-success (non-2xx) HTTP response.
    /// Confirmed against the live API on 2026-07-18: error responses are shaped
    /// <c>{"success":false,"error":{"code":"...","message":"..."},"timestamp":"..."}</c>.
    /// </summary>
    public class SendAfricaApiException : Exception
    {
        /// <summary>The HTTP status code returned by the API.</summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>The raw response body returned by the API, if any.</summary>
        public string? ResponseBody { get; }

        /// <summary>The API's machine-readable error code (e.g. "invalid_api_key"), if the body was parseable.</summary>
        public string? ErrorCode { get; }

        /// <summary>Creates a new <see cref="SendAfricaApiException"/>, parsing the API's structured error body if present.</summary>
        public SendAfricaApiException(HttpStatusCode statusCode, string? responseBody)
            : base(BuildMessage(statusCode, responseBody, out string? errorCode, out string? apiMessage))
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
            ErrorCode = errorCode;
            _ = apiMessage; // already folded into the exception Message
        }

        private static string BuildMessage(HttpStatusCode statusCode, string? responseBody, out string? errorCode, out string? apiMessage)
        {
            errorCode = null;
            apiMessage = null;

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    var envelope = JsonSerializer.Deserialize<ErrorEnvelope>(responseBody!);
                    errorCode = envelope?.Error?.Code;
                    apiMessage = envelope?.Error?.Message;
                }
                catch (JsonException)
                {
                    // Body wasn't the expected shape; fall back to the raw text below.
                }
            }

            return apiMessage is not null
                ? $"SendAfrica API request failed with status {(int)statusCode} ({statusCode}): [{errorCode}] {apiMessage}"
                : $"SendAfrica API request failed with status {(int)statusCode} ({statusCode}). Response: {responseBody}";
        }

        private class ErrorEnvelope
        {
            [JsonPropertyName("error")]
            public ErrorDetail? Error { get; set; }
        }

        private class ErrorDetail
        {
            [JsonPropertyName("code")]
            public string? Code { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }
    }
}
