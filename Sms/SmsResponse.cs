using System.Text.Json.Serialization;

namespace SendAfrica.Sms
{
    /// <summary>Response body returned after sending an SMS.</summary>
    public class SmsResponse
    {
        /// <summary>Unique identifier assigned to the message by SendAfrica.</summary>
        // TODO(API): confirm field name against the real SendAfrica API contract.
        [JsonPropertyName("messageId")]
        public string? MessageId { get; set; }

        /// <summary>Delivery/queue status reported by the API, e.g. "queued".</summary>
        // TODO(API): confirm field name against the real SendAfrica API contract.
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>Cost charged for sending this message, in the API's billing currency.</summary>
        // TODO(API): confirm field name and unit/currency against the real SendAfrica API contract.
        [JsonPropertyName("cost")]
        public decimal? Cost { get; set; }
    }
}
