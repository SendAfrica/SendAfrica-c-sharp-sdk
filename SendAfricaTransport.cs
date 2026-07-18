using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SendAfrica
{
    /// <summary>
    /// Internal HTTP transport shared by all resources. Handles retry/backoff,
    /// request tracing headers, unwrapping the <c>{success,data,error,timestamp}</c>
    /// envelope every SendAfrica API response uses, and mapping non-2xx responses
    /// to the typed <see cref="SendAfricaException"/> hierarchy. Ported from the
    /// Python SDK's <c>http.py</c>.
    /// </summary>
    internal class SendAfricaTransport
    {
        private static readonly HashSet<HttpStatusCode> RetryableStatuses = new()
        {
            (HttpStatusCode)429, HttpStatusCode.InternalServerError, HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable, HttpStatusCode.GatewayTimeout,
        };

        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            // Default System.Text.Json escaping turns "+" into a + unicode
            // escape — harmless to any conformant JSON parser, but makes request
            // bodies unreadable in debug logs. Relaxed escaping keeps "+255..." literal.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly int _maxRetries;
        private readonly bool _debug;

        // Absolute URLs are built by simple string concatenation (base + path),
        // matching the Python SDK's http.py, rather than relying on
        // HttpClient.BaseAddress + a relative Uri: a relative Uri starting with
        // "/" silently drops the base's path segment (e.g. combining
        // "https://host/v1/" with "/sms" yields "https://host/sms", not
        // "https://host/v1/sms"). Not worth re-discovering that the hard way.
        internal SendAfricaTransport(HttpClient httpClient, string apiKey, string baseUrl, int maxRetries, bool debug)
        {
            _httpClient = httpClient;
            _baseUrl = baseUrl.TrimEnd('/');
            _maxRetries = maxRetries;
            _debug = debug;

            if (_httpClient.DefaultRequestHeaders.Authorization is null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

        internal async Task<T?> RequestAsync<T>(
            HttpMethod method,
            string path,
            object? jsonBody = null,
            IDictionary<string, string>? query = null,
            CancellationToken cancellationToken = default)
        {
            string url = BuildUrl(path, query);
            int attempt = 0;
            Exception? lastException = null;

            while (attempt <= _maxRetries)
            {
                attempt++;
                string requestId = Guid.NewGuid().ToString();

                using var request = new HttpRequestMessage(method, url);
                request.Headers.TryAddWithoutValidation("X-Request-Id", requestId);
                request.Headers.TryAddWithoutValidation("User-Agent", "sendafrica-dotnet/1.0");
                if (jsonBody is not null)
                {
                    string json = JsonSerializer.Serialize(jsonBody, jsonBody.GetType(), JsonOptions);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                Log($"{method} {path} attempt={attempt} request_id={requestId}");

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    if (attempt > _maxRetries)
                    {
                        throw new ApiConnectionException($"Connection to SendAfrica API failed: {ex.Message}", ex);
                    }
                    await Task.Delay(BackoffDelay(attempt), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                Log($"status={(int)response.StatusCode}");

                if (RetryableStatuses.Contains(response.StatusCode) && attempt <= _maxRetries)
                {
                    TimeSpan delay = response.StatusCode == (HttpStatusCode)429 && response.Headers.RetryAfter?.Delta is TimeSpan retryDelta
                        ? retryDelta
                        : BackoffDelay(attempt);
                    response.Dispose();
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return await HandleResponseAsync<T>(response, requestId, cancellationToken).ConfigureAwait(false);
            }

            throw new ApiConnectionException($"Request failed after {_maxRetries} retries: {lastException?.Message}", lastException);
        }

        private static TimeSpan BackoffDelay(int attempt) =>
            TimeSpan.FromSeconds(Math.Min(0.5 * Math.Pow(2, attempt - 1), 8.0));

        private async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response, string requestId, CancellationToken cancellationToken)
        {
            string body = await response.Content.ReadAsStringAsync(
#if NET8_0_OR_GREATER
                cancellationToken
#endif
            ).ConfigureAwait(false);

            string? echoedRequestId = response.Headers.TryGetValues("X-Request-Id", out var values)
                ? System.Linq.Enumerable.FirstOrDefault(values)
                : requestId;

            JsonElement? root = null;
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    root = JsonSerializer.Deserialize<JsonElement>(body, JsonOptions);
                }
                catch (JsonException)
                {
                    // Non-JSON body; fall through with root == null.
                }
            }

            if (response.IsSuccessStatusCode)
            {
                if (root is { } rootEl)
                {
                    if (rootEl.ValueKind == JsonValueKind.Object && rootEl.TryGetProperty("data", out var dataEl))
                    {
                        return dataEl.Deserialize<T>(JsonOptions);
                    }
                    return rootEl.Deserialize<T>(JsonOptions);
                }
                return default;
            }

            string message = ExtractErrorMessage(root, (int)response.StatusCode);
            string? errorCode = ExtractErrorCode(root);
            TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta;

            throw SendAfricaException.ForStatus(response.StatusCode, message, echoedRequestId, body, errorCode, retryAfter);
        }

        private static string ExtractErrorMessage(JsonElement? root, int statusCode)
        {
            if (root is { ValueKind: JsonValueKind.Object } rootEl)
            {
                if (rootEl.TryGetProperty("error", out var errorEl) && errorEl.ValueKind == JsonValueKind.Object
                    && errorEl.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
                {
                    return msgEl.GetString() ?? $"HTTP {statusCode}";
                }
                if (rootEl.TryGetProperty("message", out var topMsgEl) && topMsgEl.ValueKind == JsonValueKind.String)
                {
                    return topMsgEl.GetString() ?? $"HTTP {statusCode}";
                }
            }
            return $"HTTP {statusCode}";
        }

        private static string? ExtractErrorCode(JsonElement? root)
        {
            if (root is { ValueKind: JsonValueKind.Object } rootEl
                && rootEl.TryGetProperty("error", out var errorEl) && errorEl.ValueKind == JsonValueKind.Object
                && errorEl.TryGetProperty("code", out var codeEl) && codeEl.ValueKind == JsonValueKind.String)
            {
                return codeEl.GetString();
            }
            return null;
        }

        private string BuildUrl(string path, IDictionary<string, string>? query)
        {
            var sb = new StringBuilder(_baseUrl);
            sb.Append(path);
            if (query is null || query.Count == 0) return sb.ToString();

            sb.Append('?');
            bool first = true;
            foreach (var kvp in query)
            {
                if (!first) sb.Append('&');
                sb.Append(Uri.EscapeDataString(kvp.Key)).Append('=').Append(Uri.EscapeDataString(kvp.Value));
                first = false;
            }
            return sb.ToString();
        }

        private void Log(string message)
        {
            if (_debug) Console.WriteLine($"[sendafrica] {message}");
        }
    }
}
