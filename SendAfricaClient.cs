using System;
using System.Net.Http;
using SendAfrica.Resources;

namespace SendAfrica
{
    /// <summary>
    /// Entry point for the SendAfrica API. Reuses a single <see cref="HttpClient"/>
    /// for all requests made through it and its resources (<see cref="Sms"/>,
    /// <see cref="Credits"/>, <see cref="Payments"/>, <see cref="Webhooks"/>).
    /// Structure mirrors the official Python SDK's <c>SendAfrica</c> client — see
    /// that project's README for the full resource-by-resource reference.
    /// </summary>
    /// <example>
    /// var client = new SendAfricaClient("SA-xxxxx");
    /// var result = await client.Sms.SendAsync("0712345678", "Welcome to SendAfrica");
    /// </example>
    public class SendAfricaClient : IDisposable
    {
        /// <summary>Environment variable checked for the API key when none is passed explicitly.</summary>
        public const string ApiKeyEnvVar = "SENDAFRICA_API_KEY";

        /// <summary>Default API base URL.</summary>
        public const string DefaultBaseUrl = "https://api.sendafrica.online/v1";

        /// <summary>Default per-request timeout, in seconds, for a client-owned <see cref="HttpClient"/>.</summary>
        public const double DefaultTimeoutSeconds = 10;

        /// <summary>Default max retry attempts on 429/5xx/connection errors.</summary>
        public const int DefaultMaxRetries = 3;

        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;

        /// <summary>Label for this client instance (read-only, for logging/display). Default "production".</summary>
        public string Environment { get; }

        /// <summary>SMS operations: send, bulk send, local cost analysis.</summary>
        public SmsResource Sms { get; }

        /// <summary>Credit balance and transaction history.</summary>
        public CreditsResource Credits { get; }

        /// <summary>Pay-as-you-go credit top-ups (vouchers) and pricing.</summary>
        public PaymentsResource Payments { get; }

        /// <summary>Incoming webhook parsing/verification (speculative — see <see cref="WebhooksResource"/>).</summary>
        public WebhooksResource Webhooks { get; }

        /// <summary>
        /// Creates a client that owns and manages its own internal <see cref="HttpClient"/>.
        /// Prefer this for simple console/worker apps. For ASP.NET apps, use the
        /// constructor overload that accepts an <see cref="HttpClient"/> from
        /// <c>IHttpClientFactory</c> instead, so socket/connection lifetime is
        /// managed for you.
        /// </summary>
        /// <param name="apiKey">
        /// Your SendAfrica API key. If null, falls back to the
        /// <c>SENDAFRICA_API_KEY</c> environment variable; throws
        /// <see cref="AuthenticationException"/> if neither is set.
        /// </param>
        /// <param name="baseUrl">Overrides the default API base URL.</param>
        /// <param name="timeoutSeconds">Per-request timeout, in seconds.</param>
        /// <param name="maxRetries">Max retry attempts on 429/5xx/connection errors.</param>
        /// <param name="environment">A label for this client instance (read-only, for logging/display).</param>
        /// <param name="debug">If true, prints <c>[sendafrica]</c> request/response logs to stdout.</param>
        /// <param name="webhookSecret">HMAC secret for webhook signature verification, if using <see cref="Webhooks"/>.</param>
        public SendAfricaClient(
            string? apiKey = null,
            string baseUrl = DefaultBaseUrl,
            double timeoutSeconds = DefaultTimeoutSeconds,
            int maxRetries = DefaultMaxRetries,
            string environment = "production",
            bool debug = false,
            string? webhookSecret = null)
            : this(
                new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) },
                apiKey,
                baseUrl,
                maxRetries,
                environment,
                debug,
                webhookSecret)
        {
            _ownsHttpClient = true;
        }

        /// <summary>
        /// Creates a client backed by a caller-supplied <see cref="HttpClient"/>.
        /// The client is never disposed by <see cref="SendAfricaClient"/> — use this
        /// overload with <c>IHttpClientFactory</c> in ASP.NET so connections are
        /// pooled and reused correctly instead of creating a new <see cref="HttpClient"/>
        /// per request.
        /// </summary>
        public SendAfricaClient(
            HttpClient httpClient,
            string? apiKey = null,
            string baseUrl = DefaultBaseUrl,
            int maxRetries = DefaultMaxRetries,
            string environment = "production",
            bool debug = false,
            string? webhookSecret = null)
        {
            if (httpClient is null) throw new ArgumentNullException(nameof(httpClient));

            string resolvedKey = ResolveApiKey(apiKey);
            _httpClient = httpClient;
            Environment = environment;

            var transport = new SendAfricaTransport(_httpClient, resolvedKey, baseUrl, maxRetries, debug);
            Sms = new SmsResource(transport);
            Credits = new CreditsResource(transport);
            Payments = new PaymentsResource(transport);
            Webhooks = new WebhooksResource(webhookSecret);
        }

        /// <summary>API key resolution: explicit argument, then <c>SENDAFRICA_API_KEY</c>, then <see cref="AuthenticationException"/>.</summary>
        private static string ResolveApiKey(string? apiKey)
        {
            if (!string.IsNullOrEmpty(apiKey)) return apiKey!;

            string? envKey = System.Environment.GetEnvironmentVariable(ApiKeyEnvVar);
            if (!string.IsNullOrEmpty(envKey)) return envKey!;

            throw new AuthenticationException(
                $"No API key provided. Pass apiKey, or set the {ApiKeyEnvVar} environment variable.");
        }

        /// <summary>Disposes the internal <see cref="HttpClient"/> if this instance created it.</summary>
        public void Dispose()
        {
            if (_ownsHttpClient)
            {
                _httpClient.Dispose();
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"<SendAfricaClient environment={Environment}>";
    }
}
