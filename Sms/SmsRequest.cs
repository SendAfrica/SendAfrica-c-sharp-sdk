using System.Text.Json.Serialization;

namespace SendAfrica.Sms
{
    /// <summary>Request body for sending an SMS.</summary>
    public class SmsRequest
    {
        /// <summary>Recipient phone number, e.g. "255712345678". Confirmed against the live API on 2026-07-18.</summary>
        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;

        /// <summary>The SMS body text. Confirmed against the live API on 2026-07-18.</summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional sender ID / short code to send from.
        /// TODO(API): the live API only requires <c>to</c> and <c>message</c> — this field's
        /// name and support are unconfirmed. Omitted from the request entirely when null.
        /// </summary>
        [JsonPropertyName("senderId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SenderId { get; set; }
    }
}
