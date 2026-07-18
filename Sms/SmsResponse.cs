using System.Text.Json.Serialization;

namespace SendAfrica.Sms
{
    /// <summary>
    /// Response returned after successfully sending an SMS. Confirmed against the
    /// live API on 2026-07-18: <c>POST https://api.sendafrica.online/v1/sms</c>
    /// returns <c>{"success":true,"data":{...},"timestamp":"..."}</c> on success;
    /// this type represents the inner <c>data</c> object.
    /// </summary>
    public class SmsResponse
    {
        /// <summary>Unique identifier assigned to the message by SendAfrica.</summary>
        [JsonPropertyName("message_id")]
        public string? MessageId { get; set; }

        /// <summary>Delivery/queue status reported by the API, e.g. "Success".</summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>Cost charged for sending this message, formatted by the API, e.g. "TZS 22.0000".</summary>
        [JsonPropertyName("cost")]
        public string? Cost { get; set; }

        /// <summary>Number of account credits consumed by this send.</summary>
        [JsonPropertyName("credits_used")]
        public int CreditsUsed { get; set; }
    }

    /// <summary>Top-level envelope every SendAfrica API response is wrapped in.</summary>
    internal class SendAfricaEnvelope<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }
    }
}
