using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SendAfrica
{
    /// <summary>Result of sending a single SMS.</summary>
    public class SmsResult
    {
        /// <summary>Server-assigned unique message ID.</summary>
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = string.Empty;

        /// <summary>Delivery/queue status reported by the API, e.g. "Success".</summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>Number of account credits consumed by this send.</summary>
        [JsonPropertyName("credits_used")]
        public int CreditsUsed { get; set; }

        /// <summary>Cost charged for sending this message, formatted by the API, e.g. "TZS 22.0000".</summary>
        [JsonPropertyName("cost")]
        public string? Cost { get; set; }

        /// <summary>The recipient phone number the message was sent to.</summary>
        [JsonPropertyName("to")]
        public string? To { get; set; }
    }

    /// <summary>
    /// Result of <see cref="Resources.SmsResource.SendManyAsync"/> — a per-message
    /// success/failure breakdown that never aborts the whole batch on one bad entry.
    /// </summary>
    public class BulkSmsResult
    {
        /// <summary>Successfully sent messages, in send order.</summary>
        public List<SmsResult> Results { get; } = new();

        /// <summary>Messages that failed to send, with their index and error.</summary>
        public List<BulkSmsFailure> Failed { get; } = new();

        /// <summary>Number of successfully sent messages.</summary>
        public int SentCount => Results.Count;

        /// <summary>Number of failed messages.</summary>
        public int FailedCount => Failed.Count;
    }

    /// <summary>One failed entry from a bulk SMS send.</summary>
    public class BulkSmsFailure
    {
        /// <summary>Position of this message in the original batch.</summary>
        public int Index { get; set; }

        /// <summary>The recipient phone number that failed, as originally supplied.</summary>
        public string? To { get; set; }

        /// <summary>The error message describing why this message failed.</summary>
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>One message in a <see cref="Resources.SmsResource.SendManyAsync"/> batch.</summary>
    public class BulkSmsMessage
    {
        /// <summary>Recipient phone number (any format <see cref="Utils.PhoneUtil.NormalizePhone"/> accepts).</summary>
        public string To { get; set; } = string.Empty;

        /// <summary>The SMS body text.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Overrides the batch-level sender for this message only.</summary>
        public string? Sender { get; set; }
    }

    /// <summary>Local-only SMS cost/encoding estimate. No network call involved.</summary>
    public class SmsAnalysis
    {
        /// <summary>"GSM-7" or "UCS-2".</summary>
        public string Encoding { get; set; } = string.Empty;

        /// <summary>Character count of the analyzed message.</summary>
        public int Characters { get; set; }

        /// <summary>Number of SMS segments the message will be split into.</summary>
        public int Parts { get; set; }

        /// <summary>Estimated credits (1 per segment). The authoritative number is <see cref="SmsResult.CreditsUsed"/>.</summary>
        public int Credits { get; set; }
    }

    /// <summary>Current account SMS credit balance.</summary>
    public class CreditBalance
    {
        /// <summary>Account identifier.</summary>
        [JsonPropertyName("account_id")]
        public string AccountId { get; set; } = string.Empty;

        /// <summary>Current credit balance.</summary>
        [JsonPropertyName("balance")]
        public int Balance { get; set; }
    }

    /// <summary>A single credit ledger entry.</summary>
    public class CreditTransaction
    {
        /// <summary>Transaction ID.</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>"debit" or "credit".</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Amount in credits.</summary>
        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        /// <summary>Balance after this transaction.</summary>
        [JsonPropertyName("balance_after")]
        public int BalanceAfter { get; set; }

        /// <summary>Human-readable description, if provided.</summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>Timestamp of the transaction.</summary>
        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }
    }

    /// <summary>A pay-as-you-go credit top-up order (a "voucher"). Credits are computed server-side from <see cref="Amount"/> at the current rate — see <see cref="VoucherRate"/>.</summary>
    public class Payment
    {
        /// <summary>Payment/order ID.</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>Payment status.</summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>Amount in TZS.</summary>
        [JsonPropertyName("amount")]
        public int? Amount { get; set; }

        /// <summary>Credits to be credited once the payment settles.</summary>
        [JsonPropertyName("credit_amount")]
        public int? CreditAmount { get; set; }

        /// <summary>Currency code. Always "TZS".</summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "TZS";

        /// <summary>Payment provider used (e.g. "manual", "snippe").</summary>
        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        /// <summary>Payment source, if reported by the API.</summary>
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        /// <summary>Timestamp the payment was created.</summary>
        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }
    }

    /// <summary>One bracket of the tiered TZS-per-credit voucher pricing schedule. <see cref="MaxAmountTzs"/> of 0 marks the top, unbounded tier.</summary>
    public class RateTier
    {
        /// <summary>Upper bound of this tier in TZS. 0 means unbounded (the top tier).</summary>
        [JsonPropertyName("max_amount_tzs")]
        public int MaxAmountTzs { get; set; }

        /// <summary>Price per credit, in TZS, within this tier.</summary>
        [JsonPropertyName("rate_tzs_per_credit")]
        public int RateTzsPerCredit { get; set; }
    }

    /// <summary>Minimum top-up amount and tiered pricing schedule for credit vouchers.</summary>
    public class VoucherRate
    {
        /// <summary>Minimum top-up amount in TZS.</summary>
        [JsonPropertyName("min_amount_tzs")]
        public int MinAmountTzs { get; set; }

        /// <summary>Pricing tiers, in ascending order.</summary>
        [JsonPropertyName("tiers")]
        public List<RateTier> Tiers { get; set; } = new();
    }

    /// <summary>
    /// A parsed incoming webhook event.
    /// Speculative: SendAfrica does not currently forward signed events to customer
    /// endpoints. See <see cref="Resources.WebhooksResource"/>.
    /// </summary>
    public class WebhookEvent
    {
        /// <summary>Event type, e.g. "sms.delivered".</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Associated message ID, if applicable to this event type.</summary>
        [JsonPropertyName("message_id")]
        public string? MessageId { get; set; }

        /// <summary>The full raw event payload.</summary>
        public Dictionary<string, object?> Data { get; set; } = new();
    }
}
