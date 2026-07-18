using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SendAfrica.Utils;

namespace SendAfrica.Resources
{
    /// <summary>SMS operations: <c>client.Sms.*</c>. Ported from the Python SDK's <c>resources/sms.py</c>.</summary>
    public class SmsResource
    {
        private readonly SendAfricaTransport _transport;

        internal SmsResource(SendAfricaTransport transport)
        {
            _transport = transport;
        }

        /// <summary>
        /// Sends a single SMS. Validates <paramref name="message"/> is non-empty and
        /// normalizes <paramref name="to"/> to E.164 locally, before any network call
        /// — bad input never reaches the wire. Throws <see cref="InvalidPhoneException"/>
        /// or <see cref="ValidationException"/> for local validation failures.
        /// </summary>
        public async Task<SmsResult> SendAsync(string to, string message, string? sender = null, CancellationToken cancellationToken = default)
        {
            Validators.Require(message, "message");

            var payload = new SmsRequestPayload
            {
                To = PhoneUtil.NormalizePhone(to),
                Message = message,
                From = Validators.ValidateSenderId(sender),
            };

            var result = await _transport.RequestAsync<SmsResult>(HttpMethod.Post, "/sms", payload, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return result ?? new SmsResult();
        }

        /// <summary>
        /// Sends multiple SMS. This is a client-side loop over <see cref="SendAsync"/>,
        /// not a call to a bulk server endpoint — it exists to give per-message
        /// success/failure without aborting the whole batch. Paces requests using
        /// <paramref name="rateLimitPerSec"/> (default 10/sec).
        /// </summary>
        public async Task<BulkSmsResult> SendManyAsync(
            IEnumerable<BulkSmsMessage> messages,
            string? sender = null,
            double rateLimitPerSec = 10.0,
            CancellationToken cancellationToken = default)
        {
            var result = new BulkSmsResult();
            var list = new List<BulkSmsMessage>(messages);
            int delayMs = rateLimitPerSec > 0 ? (int)(1000.0 / rateLimitPerSec) : 0;

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                try
                {
                    var sent = await SendAsync(item.To, item.Message, item.Sender ?? sender, cancellationToken).ConfigureAwait(false);
                    result.Results.Add(sent);
                }
                catch (System.Exception exc)
                {
                    result.Failed.Add(new BulkSmsFailure { Index = i, To = item.To, Error = exc.Message });
                }

                if (delayMs > 0 && i < list.Count - 1)
                {
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
            }

            return result;
        }

        /// <summary>
        /// Previews encoding, segment count, and credit cost for <paramref name="message"/>
        /// with zero network calls. The authoritative credit count is
        /// <see cref="SmsResult.CreditsUsed"/> from <see cref="SendAsync"/>.
        /// </summary>
        public SmsAnalysis Analyze(string message) => SmsAnalyzer.Analyze(message);

        private class SmsRequestPayload
        {
            [JsonPropertyName("to")]
            public string To { get; set; } = string.Empty;

            [JsonPropertyName("message")]
            public string Message { get; set; } = string.Empty;

            // Wire field for the sender ID is "from" — matches the Go API's
            // SendSMSRequest.From field. Do not rename this back to "senderId".
            [JsonPropertyName("from")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? From { get; set; }
        }
    }
}
