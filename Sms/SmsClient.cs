using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SendAfrica.Sms
{
    /// <summary>SMS operations for the SendAfrica API.</summary>
    public class SmsClient
    {
        // Confirmed against the live API on 2026-07-18.
        private const string SendPath = "v1/sms";

        private readonly HttpClient _httpClient;

        internal SmsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Sends an SMS message.
        /// </summary>
        /// <param name="request">The message to send.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The API's response describing the sent message.</returns>
        /// <exception cref="SendAfricaApiException">
        /// Thrown when the API returns a non-success (non-2xx) status code.
        /// </exception>
        public async Task<SmsResponse> SendAsync(SmsRequest request, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _httpClient
                .PostAsJsonAsync(SendPath, request, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(
#if NET8_0_OR_GREATER
                    cancellationToken
#endif
                ).ConfigureAwait(false);
                throw new SendAfricaApiException(response.StatusCode, body);
            }

            SendAfricaEnvelope<SmsResponse>? envelope = await response.Content
                .ReadFromJsonAsync<SendAfricaEnvelope<SmsResponse>>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return envelope?.Data ?? new SmsResponse();
        }
    }
}
