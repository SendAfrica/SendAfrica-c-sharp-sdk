using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SendAfrica.Resources
{
    /// <summary>Credit balance and history: <c>client.Credits.*</c>. Ported from the Python SDK's <c>resources/credits.py</c>.</summary>
    public class CreditsResource
    {
        private readonly SendAfricaTransport _transport;

        internal CreditsResource(SendAfricaTransport transport)
        {
            _transport = transport;
        }

        /// <summary>Fetches the current account SMS credit balance.</summary>
        public async Task<CreditBalance> BalanceAsync(CancellationToken cancellationToken = default)
        {
            var result = await _transport.RequestAsync<CreditBalance>(HttpMethod.Get, "/credits/balance", cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return result ?? new CreditBalance();
        }

        /// <summary>
        /// Lists credit transactions. Paginated with <paramref name="page"/>/<paramref name="perPage"/>
        /// (page-based, not cursor-based — matches what <c>GET /v1/credits/history</c> actually accepts).
        /// </summary>
        public async Task<List<CreditTransaction>> HistoryAsync(int page = 1, int perPage = 25, CancellationToken cancellationToken = default)
        {
            var query = new Dictionary<string, string>
            {
                ["page"] = page.ToString(),
                ["per_page"] = perPage.ToString(),
            };

            var result = await _transport.RequestAsync<CreditHistoryPage>(HttpMethod.Get, "/credits/history", query: query, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return result?.Items ?? new List<CreditTransaction>();
        }

        private class CreditHistoryPage
        {
            [JsonPropertyName("items")]
            public List<CreditTransaction> Items { get; set; } = new();
        }
    }
}
