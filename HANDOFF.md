# Handoff: real API details needed (for Lugenge)

This scaffold compiles, packs, and installs, but every value tied to the
actual SendAfrica API is a placeholder. Search the repo for `TODO(API):` to
find every spot; the list below is the same set with what's needed for each.

## Checklist

- [ ] **Base URL** — [SendAfricaClient.cs:14](SendAfricaClient.cs#L14). Currently
      `https://api.sendafrica.online` (unverified guess).
- [ ] **Auth header format** — [SendAfricaClient.cs:59](SendAfricaClient.cs#L59).
      Currently assumes `Authorization: Bearer <apiKey>`. Confirm whether it's
      Bearer, a custom header (e.g. `X-API-Key`), or a query param.
- [ ] **SMS send endpoint path** — [Sms/SmsClient.cs:11](Sms/SmsClient.cs#L11).
      Currently `v1/sms` (unverified guess — could be `v1/sms/send` or similar).
- [ ] **SmsRequest JSON field names** — [Sms/SmsRequest.cs](Sms/SmsRequest.cs).
      Currently `to`, `message`, `senderId`.
- [ ] **SmsResponse JSON field names** — [Sms/SmsResponse.cs](Sms/SmsResponse.cs).
      Currently `messageId`, `status`, `cost`. Also confirm the `cost` field's
      currency/unit.
- [ ] **Sandbox/test API key** — needed to actually exercise the client against
      the real API and replace this scaffold's untested assumptions.
- [ ] **Error response shape** — confirm whether non-2xx responses return a
      JSON error body (and its shape) so `SendAfricaApiException` can surface
      a structured message instead of just the raw body text.
- [ ] **NuGet publish ownership** — who holds the nuget.org API key and account
      for the `SendAfrica` package ID. Confirm the ID isn't already taken.

## What's already solid (no changes needed)

- Dual targeting `net8.0;netstandard2.0`.
- Single shared `HttpClient`, with an `IHttpClientFactory`-friendly constructor
  overload for ASP.NET usage.
- `SendAfricaApiException` wraps non-2xx responses with status code + body.
- `.csproj` packaging metadata (license, tags, docs, symbols) is filled in.

## Once the real details are confirmed

1. Update the `TODO(API):` spots above.
2. Delete the corresponding line from this checklist (or delete this whole
   file once nothing's left).
3. Re-run `dotnet build -c Release && dotnet pack -c Release` and smoke-test
   against the real sandbox API key before publishing.
