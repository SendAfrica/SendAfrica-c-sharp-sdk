using System;
using System.Net.Http;
using System.Net.Http.Headers;
using SendAfrica.Sms;

namespace SendAfrica
{
    /// <summary>
    /// Entry point for the SendAfrica API. Reuses a single <see cref="HttpClient"/>
    /// for all requests made through it and its sub-clients (e.g. <see cref="Sms"/>).
    /// </summary>
    public class SendAfricaClient : IDisposable
    {
        // Confirmed against the live API on 2026-07-18.
        internal const string DefaultBaseUrl = "https://api.sendafrica.online";

        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;

        /// <summary>SMS operations (send, etc.).</summary>
        public SmsClient Sms { get; }

        /// <summary>
        /// Creates a client that owns and manages its own internal <see cref="HttpClient"/>.
        /// Prefer this for simple console/worker apps. For ASP.NET apps, use the
        /// constructor overload that accepts an <see cref="HttpClient"/> from
        /// <c>IHttpClientFactory</c> instead, so socket/connection lifetime is managed
        /// for you.
        /// </summary>
        /// <param name="apiKey">Your SendAfrica API key.</param>
        /// <param name="baseUrl">Overrides the default API base URL. Optional.</param>
        public SendAfricaClient(string apiKey, string? baseUrl = null)
            : this(new HttpClient(), apiKey, baseUrl)
        {
            _ownsHttpClient = true;
        }

        /// <summary>
        /// Creates a client backed by a caller-supplied <see cref="HttpClient"/>.
        /// The client is never disposed by <see cref="SendAfricaClient"/> — use this
        /// overload with <c>IHttpClientFactory</c> in ASP.NET so connections are pooled
        /// and reused correctly instead of creating a new <see cref="HttpClient"/> per request.
        /// </summary>
        /// <param name="httpClient">An externally managed <see cref="HttpClient"/> instance.</param>
        /// <param name="apiKey">Your SendAfrica API key.</param>
        /// <param name="baseUrl">Overrides the default API base URL. Optional.</param>
        public SendAfricaClient(HttpClient httpClient, string apiKey, string? baseUrl = null)
        {
            if (httpClient is null) throw new ArgumentNullException(nameof(httpClient));
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key must not be empty.", nameof(apiKey));

            _httpClient = httpClient;

            if (_httpClient.BaseAddress is null)
            {
                _httpClient.BaseAddress = new Uri(baseUrl ?? DefaultBaseUrl);
            }

            // Confirmed against the live API on 2026-07-18: the server accepts the key via
            // either "Authorization: Bearer <key>" or "X-API-Key: <key>". Bearer is used here.
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            Sms = new SmsClient(_httpClient);
        }

        /// <summary>Disposes the internal <see cref="HttpClient"/> if this instance created it.</summary>
        public void Dispose()
        {
            if (_ownsHttpClient)
            {
                _httpClient.Dispose();
            }
        }
    }
}
