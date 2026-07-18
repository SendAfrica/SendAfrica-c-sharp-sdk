using System.Text.Json.Serialization;

namespace SendAfrica.Sms
{
    /// <summary>Request body for sending an SMS.</summary>
    public class SmsRequest
    {
        /// <summary>Recipient phone number, e.g. "0712345678".</summary>
        // TODO(API): confirm field name against the real SendAfrica API contract.
        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;

        /// <summary>The SMS body text.</summary>
        // TODO(API): confirm field name against the real SendAfrica API contract.
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>Optional sender ID / short code to send from.</summary>
        // TODO(API): confirm field name against the real SendAfrica API contract.
        [JsonPropertyName("senderId")]
        public string? SenderId { get; set; }
    }
}
