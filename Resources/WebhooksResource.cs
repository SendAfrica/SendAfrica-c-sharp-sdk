using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SendAfrica.Resources
{
    /// <summary>
    /// Incoming webhook parsing/verification: <c>client.Webhooks.Parse(...)</c>.
    /// Ported from the Python SDK's <c>resources/webhooks.py</c>.
    /// </summary>
    /// <remarks>
    /// <b>Speculative.</b> As of this writing, the SendAfrica API has no mechanism
    /// that forwards signed events (e.g. delivery-status changes) to a customer's
    /// own endpoint. This resource is provided ahead of that backend feature
    /// shipping. Signature verification uses HMAC-SHA256 over the raw body,
    /// matching the common pattern used by Stripe/most SMS aggregators — adjust
    /// the header name/scheme once SendAfrica's outbound webhook spec is finalized.
    /// </remarks>
    public class WebhooksResource
    {
        /// <summary>Header a SendAfrica webhook signature would arrive in, once outbound webhooks ship.</summary>
        public const string SignatureHeader = "X-SendAfrica-Signature";

        private readonly string? _webhookSecret;

        internal WebhooksResource(string? webhookSecret)
        {
            _webhookSecret = webhookSecret;
        }

        /// <summary>
        /// Parses (and, if a signature/secret are available, verifies) an incoming
        /// webhook payload. Throws <see cref="WebhookSignatureException"/> on a
        /// signature mismatch so callers can reject the request without processing it.
        /// </summary>
        public WebhookEvent Parse(string payload, string? signature = null, string? secret = null)
        {
            secret ??= _webhookSecret;
            if (!string.IsNullOrEmpty(signature) && !string.IsNullOrEmpty(secret))
            {
                VerifySignature(payload, signature!, secret!);
            }

            using JsonDocument doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var data = new Dictionary<string, object?>();
            foreach (var prop in root.EnumerateObject())
            {
                data[prop.Name] = ToPlainValue(prop.Value);
            }

            return new WebhookEvent
            {
                Type = root.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String ? typeEl.GetString()! : "",
                MessageId = root.TryGetProperty("message_id", out var idEl) && idEl.ValueKind == JsonValueKind.String ? idEl.GetString() : null,
                Data = data,
            };
        }

        private static void VerifySignature(string payload, string signature, string secret)
        {
            byte[] key = Encoding.UTF8.GetBytes(secret);
            byte[] message = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(key);
            byte[] computed = hmac.ComputeHash(message);
            string expected = BytesToHex(computed);

            if (!FixedTimeEquals(expected, signature))
            {
                throw new WebhookSignatureException("Webhook signature verification failed");
            }
        }

        private static string BytesToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        // Constant-time comparison to avoid a timing side-channel — the whole
        // point of signature verification. Do not replace with ==/string.Equals.
        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }

        private static object? ToPlainValue(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.Object => EnumerateObjectToDict(element),
            JsonValueKind.Array => EnumerateArrayToList(element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };

        private static Dictionary<string, object?> EnumerateObjectToDict(JsonElement element)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in element.EnumerateObject())
            {
                dict[prop.Name] = ToPlainValue(prop.Value);
            }
            return dict;
        }

        private static List<object?> EnumerateArrayToList(JsonElement element)
        {
            var list = new List<object?>();
            foreach (var item in element.EnumerateArray())
            {
                list.Add(ToPlainValue(item));
            }
            return list;
        }
    }
}
