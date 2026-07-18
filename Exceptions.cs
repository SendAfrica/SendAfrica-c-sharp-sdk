using System;
using System.Net;

namespace SendAfrica
{
    /// <summary>
    /// Base class for all SendAfrica SDK errors. Mirrors the exception hierarchy
    /// of the official Python SDK (<c>SendAfricaError</c>), so error handling
    /// patterns translate directly.
    /// </summary>
    public class SendAfricaException : Exception
    {
        /// <summary>HTTP status code, if this error came from an API response.</summary>
        public int? StatusCode { get; }

        /// <summary>The <c>X-Request-Id</c> value for the request that failed, if available.</summary>
        public string? RequestId { get; }

        /// <summary>The raw response body, if this error came from an API response.</summary>
        public string? ResponseBody { get; }

        /// <summary>The API's machine-readable error code (e.g. "invalid_api_key"), if present.</summary>
        public string? ErrorCode { get; }

        /// <summary>Creates a new <see cref="SendAfricaException"/>.</summary>
        public SendAfricaException(
            string message,
            int? statusCode = null,
            string? requestId = null,
            string? responseBody = null,
            string? errorCode = null,
            Exception? innerException = null)
            : base(BuildMessage(message, statusCode, requestId), innerException)
        {
            StatusCode = statusCode;
            RequestId = requestId;
            ResponseBody = responseBody;
            ErrorCode = errorCode;
        }

        private static string BuildMessage(string message, int? statusCode, string? requestId)
        {
            var parts = new System.Collections.Generic.List<string> { message };
            if (statusCode is not null) parts.Add($"(status={statusCode})");
            if (!string.IsNullOrEmpty(requestId)) parts.Add($"(request_id={requestId})");
            return string.Join(" ", parts);
        }

        /// <summary>Maps an HTTP status code to the appropriate <see cref="SendAfricaException"/> subtype, matching STATUS_CODE_MAP in the Python SDK.</summary>
        internal static SendAfricaException ForStatus(
            HttpStatusCode statusCode,
            string message,
            string? requestId,
            string? responseBody,
            string? errorCode,
            TimeSpan? retryAfter = null)
        {
            int code = (int)statusCode;
            return code switch
            {
                400 or 422 => new ValidationException(message, code, requestId, responseBody, errorCode),
                401 => new AuthenticationException(message, code, requestId, responseBody, errorCode),
                402 => new InsufficientCreditsException(message, code, requestId, responseBody, errorCode),
                404 => new NotFoundException(message, code, requestId, responseBody, errorCode),
                429 => new RateLimitException(message, code, requestId, responseBody, errorCode, retryAfter),
                >= 500 and < 600 => new ServerException(message, code, requestId, responseBody, errorCode),
                _ => new SendAfricaException(message, code, requestId, responseBody, errorCode),
            };
        }
    }

    /// <summary>Raised for invalid/missing API keys (HTTP 401).</summary>
    public class AuthenticationException : SendAfricaException
    {
        /// <summary>Creates a new <see cref="AuthenticationException"/>.</summary>
        public AuthenticationException(string message, int? statusCode = null, string? requestId = null, string? responseBody = null, string? errorCode = null)
            : base(message, statusCode, requestId, responseBody, errorCode) { }
    }

    /// <summary>Raised when the API rejects the payload as invalid (HTTP 400/422).</summary>
    public class ValidationException : SendAfricaException
    {
        /// <summary>Creates a new <see cref="ValidationException"/>.</summary>
        public ValidationException(string message, int? statusCode = null, string? requestId = null, string? responseBody = null, string? errorCode = null)
            : base(message, statusCode, requestId, responseBody, errorCode) { }
    }

    /// <summary>Raised when a phone number fails local or server-side validation.</summary>
    public class InvalidPhoneException : ValidationException
    {
        /// <summary>Creates a new <see cref="InvalidPhoneException"/>.</summary>
        public InvalidPhoneException(string message, int? statusCode = null, string? requestId = null, string? responseBody = null, string? errorCode = null)
            : base(message, statusCode, requestId, responseBody, errorCode) { }
    }

    /// <summary>Raised when the account does not have enough SMS credits (HTTP 402).</summary>
    public class InsufficientCreditsException : SendAfricaException
    {
        /// <summary>Creates a new <see cref="InsufficientCreditsException"/>.</summary>
        public InsufficientCreditsException(string message, int? statusCode = null, string? requestId = null, string? responseBody = null, string? errorCode = null)
            : base(message, statusCode, requestId, responseBody, errorCode) { }
    }

    /// <summary>Raised when the caller has been rate limited (HTTP 429).</summary>
    public class RateLimitException : SendAfricaException
    {
        /// <summary>Seconds to wait before retrying, from the <c>Retry-After</c> header, if present.</summary>
        public TimeSpan? RetryAfter { get; }

        /// <summary>Creates a new <see cref="RateLimitException"/>.</summary>
        public RateLimitException(string message, int? statusCode = null, string? requestId = null, string? responseBody = null, string? errorCode = null, TimeSpan? retryAfter = null)
            : base(message, statusCode, requestId, responseBody, errorCode)
        {
            RetryAfter = retryAfter;
        }
    }

    /// <summary>Raised when a resource (payment, package, message) does not exist (HTTP 404).</summary>
    public class NotFoundException : SendAfricaException
    {
        /// <summary>Creates a new <see cref="NotFoundException"/>.</summary>
        public NotFoundException(string message, int? statusCode = null, string? requestId = null, string? responseBody = null, string? errorCode = null)
            : base(message, statusCode, requestId, responseBody, errorCode) { }
    }

    /// <summary>Raised for 5xx responses from the SendAfrica API.</summary>
    public class ServerException : SendAfricaException
    {
        /// <summary>Creates a new <see cref="ServerException"/>.</summary>
        public ServerException(string message, int? statusCode = null, string? requestId = null, string? responseBody = null, string? errorCode = null)
            : base(message, statusCode, requestId, responseBody, errorCode) { }
    }

    /// <summary>Raised for network-level failures (timeouts, DNS, connection refused).</summary>
    public class ApiConnectionException : SendAfricaException
    {
        /// <summary>Creates a new <see cref="ApiConnectionException"/>.</summary>
        public ApiConnectionException(string message, Exception? innerException = null)
            : base(message, innerException: innerException) { }
    }

    /// <summary>Raised when an incoming webhook payload fails signature verification.</summary>
    public class WebhookSignatureException : SendAfricaException
    {
        /// <summary>Creates a new <see cref="WebhookSignatureException"/>.</summary>
        public WebhookSignatureException(string message) : base(message) { }
    }
}
