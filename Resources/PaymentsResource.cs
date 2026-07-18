using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SendAfrica.Utils;

namespace SendAfrica.Resources
{
    /// <summary>
    /// Credit top-up (voucher) operations: <c>client.Payments.*</c>. Ported from
    /// the Python SDK's <c>resources/payments.py</c>.
    /// </summary>
    /// <remarks>
    /// Wraps the pay-as-you-go voucher endpoints — <c>POST /v1/vouchers</c> and
    /// <c>GET /v1/vouchers/rate</c> — not the fixed-package <c>POST /v1/payments</c>
    /// endpoint (that endpoint exists API-side but isn't wrapped here; see the
    /// Python SDK's resources README for the reasoning). There is intentionally no
    /// <c>Get</c>/<c>List</c>: order lookup/listing is an admin-console, JWT-only
    /// feature, unreachable with an API key.
    /// </remarks>
    public class PaymentsResource
    {
        private readonly SendAfricaTransport _transport;

        internal PaymentsResource(SendAfricaTransport transport)
        {
            _transport = transport;
        }

        /// <summary>
        /// Initiates a pay-as-you-go top-up for <paramref name="amount"/> (TZS).
        /// <paramref name="phone"/> is required for mobile-money providers (e.g.
        /// "snippe") but optional for "manual". Call <see cref="RateAsync"/> first
        /// if you want to validate the amount against the minimum client-side.
        /// </summary>
        public async Task<Payment> CreateAsync(int amount, string provider = "manual", string? phone = null, CancellationToken cancellationToken = default)
        {
            Validators.Require(provider, "provider");
            Validators.ValidatePositiveAmount(amount);

            var payload = new PaymentRequestPayload
            {
                Provider = provider,
                Amount = amount,
                Phone = phone is not null ? PhoneUtil.NormalizePhone(phone) : null,
            };

            var result = await _transport.RequestAsync<Payment>(HttpMethod.Post, "/vouchers", payload, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return result ?? new Payment();
        }

        /// <summary>
        /// Fetches the minimum top-up amount and the tiered TZS-per-credit pricing
        /// schedule, so you can display pricing or validate an amount before
        /// calling <see cref="CreateAsync"/>.
        /// </summary>
        public async Task<VoucherRate> RateAsync(CancellationToken cancellationToken = default)
        {
            var result = await _transport.RequestAsync<VoucherRate>(HttpMethod.Get, "/vouchers/rate", cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return result ?? new VoucherRate();
        }

        private class PaymentRequestPayload
        {
            [JsonPropertyName("provider")]
            public string Provider { get; set; } = "manual";

            [JsonPropertyName("amount")]
            public int Amount { get; set; }

            [JsonPropertyName("phone")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Phone { get; set; }
        }
    }
}
