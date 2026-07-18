# Handoff: API integration status (for Lugenge)

## Confirmed against the live API (2026-07-18)

**SMS only.** Verified directly against `https://api.sendafrica.online`
using a real account API key:

- **Base URL:** `https://api.sendafrica.online/v1`
- **SMS send endpoint:** `POST /sms`
- **Auth:** either `Authorization: Bearer <key>` or `X-API-Key: <key>` header.
  The SDK uses `Bearer`.
- **Request body:** `{"to": "...", "message": "...", "from": "..."}` — `to`/
  `message` required, `from` (sender ID) unconfirmed (see below).
- **Success response:** `{"success":true,"data":{"message_id","status","cost","credits_used"},"timestamp"}`
- **Error response:** `{"success":false,"error":{"code","message"},"timestamp"}`

## Ported, not independently confirmed

Credits, Payments, and Webhooks were **not** probed against the live API by
this SDK. They're ported from the official Python SDK
(`SendAfrica/SendAfrica-python-sdk`), which does have this reasoning baked
into its own `resources/README.md` — worth reading directly if you touch
these:

- **Credits** (`GET /credits/balance`, `GET /credits/history`) — history
  uses `page`/`per_page` query params (not cursor-based); the Python SDK's
  notes explicitly warn against changing this without checking the Go API's
  `apps/credits/handler.go` — an earlier version of that SDK got it wrong.
- **Payments** (`POST /vouchers`, `GET /vouchers/rate`) — wraps the
  pay-as-you-go voucher endpoints only, not the fixed-package
  `POST /payments` endpoint (that endpoint exists but isn't wrapped by
  either SDK). No `Get`/`List`: order lookup/listing is admin-JWT-only,
  unreachable with an API key — don't add it without confirming the API
  actually exposes an API-key-auth equivalent.
- **Webhooks** — genuinely speculative on both SDKs. SendAfrica does not
  currently forward signed events to customer endpoints; this resource
  (HMAC verification + parsing) is local-only and ready for when that ships.

Before relying on Credits/Payments in production: exercise each method once
against the live API with a real key and confirm the response shape matches
`Models.cs`. Low cost to check (`BalanceAsync`/`RateAsync` are `GET`s, free
and side-effect-free); `CreateAsync` on Payments only *initiates* a top-up,
it doesn't charge anything by itself.

## Still open

- [ ] **`from` (sender ID) field on SMS** — [Resources/SmsResource.cs](Resources/SmsResource.cs).
      The live API only validated `to`/`message` when this was checked; sending
      `from` is untested. Field name confirmed via the Python SDK's source
      comments (`Go API's SendSMSRequest.From`), not independently verified
      by this SDK against the live API.
- [ ] **Rate limit behavior** — retry/backoff for 429/5xx is implemented
      (ported from the Python SDK), but never exercised against an actual
      429 response from the live API.
- [ ] **CLI tool** — the Python SDK ships a `sendafrica` CLI command
      (balance checks, quick sends from the terminal). Not ported here;
      would need its own packaging as a .NET global tool if wanted.
- [x] **NuGet publish ownership** — resolved 2026-07-18. The `SendAfrica`
      package ID was unclaimed and is now published under the `Prosaic`
      nuget.org account, first as v1.0.0 then v1.0.1. Whoever holds that
      account's login is who can publish future versions or grant others
      push access via a scoped API key.

## Important: how live testing must be done from here on

Confirming the SMS endpoint cost 2 real credits because a plausible-looking
phone number was used instead of a genuinely invalid one — **do not repeat
that.** Any further live testing must either:
- Use a number you own and expect to receive a text on, with explicit
  awareness it will send and cost a credit, or
- Check with SendAfrica support/docs for an official sandbox mode or
  designated test number before sending anything automated.

`Credits.BalanceAsync()`/`Payments.RateAsync()` are safe to call freely —
they're `GET`s with no side effects and don't cost credits.

## What's already solid (no changes needed)

- Dual targeting `net8.0;netstandard2.0`.
- Single shared `HttpClient`, with an `IHttpClientFactory`-friendly
  constructor overload for ASP.NET usage.
- Full exception hierarchy (`SendAfricaException` and 8 subtypes) mirroring
  the Python SDK, with status-code mapping and `RetryAfter` on rate limits.
- Retry/backoff on 429/5xx/connection errors, matching the Python SDK's
  timing (`min(0.5 * 2^(attempt-1), 8.0)`s, max 3 retries).
- Local phone normalization (E.164) and SMS cost/encoding analysis — zero
  network calls, ported faithfully from `utils/phone.py` and `utils/sms.py`.
- `.csproj` packaging metadata (license, tags, docs, symbols) is filled in.
- 22-case offline verification suite (stubbed HTTP responses, no live
  calls) covering every resource, error mapping, phone normalization, and
  webhook signature verification — all passing as of this handoff.
