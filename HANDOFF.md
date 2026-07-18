# Handoff: API integration status (for Lugenge)

## Confirmed against the live API (2026-07-18)

These were verified directly against `https://api.sendafrica.online` using a
real API key and are now reflected in the code — no longer placeholders:

- **Base URL:** `https://api.sendafrica.online`
- **SMS send endpoint:** `POST /v1/sms`
- **Auth:** either `Authorization: Bearer <key>` or `X-API-Key: <key>` header.
  The SDK uses `Bearer`.
- **Request body:** `{"to": "...", "message": "..."}` — both required; the API
  returns `400 validation_error` if either is missing.
- **Success response:**
  ```json
  {
    "success": true,
    "data": {
      "message_id": "SA-...",
      "status": "Success",
      "cost": "TZS 22.0000",
      "credits_used": 1
    },
    "timestamp": "..."
  }
  ```
- **Error response:**
  ```json
  {
    "success": false,
    "error": { "code": "invalid_api_key", "message": "Invalid or inactive API key" },
    "timestamp": "..."
  }
  ```
  `SendAfricaApiException` now parses this into `.ErrorCode` and a readable
  `.Message`.

## Still open

- [ ] **`senderId` support** — [Sms/SmsRequest.cs](Sms/SmsRequest.cs). The API
      only validates `to`/`message`; whether a sender ID / short code field is
      supported (and its real field name) is unconfirmed. Currently sent as
      `senderId` only when set, omitted otherwise — confirm or remove.
- [ ] **Payments / Credits endpoints** — not built yet. This scaffold only
      covers SMS. Add `Payments/` and `Credits/` clients once those endpoints
      are documented or confirmed the same way SMS was.
- [ ] **Rate limits / retry behavior** — unconfirmed. If the API has rate
      limits, the SDK doesn't currently implement retry/backoff.
- [ ] **NuGet publish ownership** — who holds the nuget.org API key and
      account for the `SendAfrica` package ID. Confirm the ID isn't already
      taken.

## Important: how "confirmed" testing must be done from here on

The SMS confirmation above cost 2 real credits because a plausible-looking
phone number was used instead of a genuinely invalid one — **do not repeat
that.** Any further live testing must either:
- Use a number you own and expect to receive a text on, with explicit
  awareness it will send and cost a credit, or
- Check with SendAfrica support/docs for an official sandbox mode or
  designated test number before sending anything automated.

## What's already solid (no changes needed)

- Dual targeting `net8.0;netstandard2.0`.
- Single shared `HttpClient`, with an `IHttpClientFactory`-friendly constructor
  overload for ASP.NET usage.
- `SendAfricaApiException` wraps non-2xx responses with status code, parsed
  error code, and message.
- `.csproj` packaging metadata (license, tags, docs, symbols) is filled in.
