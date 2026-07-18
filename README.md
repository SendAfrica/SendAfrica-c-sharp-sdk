# SendAfrica

Official C# SDK for the SendAfrica SMS & Payments API.

## Status (as of 2026-07-18)

**SMS sending is implemented and confirmed working against the live API.**
Payments/Credits are not built yet. See [HANDOFF.md](HANDOFF.md) for the full
open-items checklist.

### What's confirmed against the live API

Verified directly against `https://api.sendafrica.online` using a real
account API key:

- **Base URL:** `https://api.sendafrica.online`
- **SMS send endpoint:** `POST /v1/sms`
- **Auth:** either `Authorization: Bearer <key>` or `X-API-Key: <key>` header
  (this SDK uses `Bearer`).
- **Request body:** `{"to": "...", "message": "..."}` — both required; the
  API returns `400 validation_error` if either is missing.
- **Success response:** `{"success":true,"data":{"message_id","status","cost","credits_used"},"timestamp"}`
- **Error response:** `{"success":false,"error":{"code","message"},"timestamp"}` —
  parsed by `SendAfricaApiException` into `.ErrorCode` and a readable message.

### What's not confirmed / not built yet

- `senderId` (optional field on `SmsRequest`) — sending it is untested; the
  API only validates `to`/`message`.
- Payments and Credits clients — no endpoints wired up yet.
- Rate limiting / retry behavior — unknown, not implemented.

Full detail and an actionable checklist: [HANDOFF.md](HANDOFF.md).

### How this was verified

A real SendAfrica API key was used to probe `api.sendafrica.online` directly
(garbage key vs. real key, several candidate paths, both auth header styles,
missing-field requests) to observe the actual responses, rather than guessing.
That confirmed the endpoint, auth, and both the success and error JSON shapes
above. One outcome of that process: two live test messages were
accidentally delivered to a real phone number (not a dummy one) during
probing, consuming 2 account credits. See the "testing" note in
[HANDOFF.md](HANDOFF.md) for how to avoid that going forward — always use a
number you own or a designated sandbox/test number for any further live
testing, never a plausible-looking but arbitrary number.

## Install

```bash
dotnet add package SendAfrica
```

## Usage

```csharp
using SendAfrica;
using SendAfrica.Sms;

var client = new SendAfricaClient("YOUR_API_KEY");

var result = await client.Sms.SendAsync(new SmsRequest
{
    To = "255712345678",
    Message = "Hello from SendAfrica"
});

Console.WriteLine($"{result.MessageId}: {result.Status} (cost: {result.Cost}, credits used: {result.CreditsUsed})");
```

Non-2xx responses throw `SendAfricaApiException`, which exposes `StatusCode`,
`ErrorCode` (e.g. `"invalid_api_key"`), and a human-readable `Message`.

## ASP.NET usage (recommended)

Register `HttpClient` via `IHttpClientFactory` instead of letting `SendAfricaClient`
create its own — this avoids socket exhaustion under load.

```csharp
// Program.cs
builder.Services.AddHttpClient<SendAfricaClient>((httpClient, sp) =>
    new SendAfricaClient(httpClient, builder.Configuration["SendAfrica:ApiKey"]!));
```

```csharp
// Wherever you need it
public class NotificationService
{
    private readonly SendAfricaClient _sendAfrica;

    public NotificationService(SendAfricaClient sendAfrica) => _sendAfrica = sendAfrica;

    public Task NotifyAsync(string phone, string message) =>
        _sendAfrica.Sms.SendAsync(new SmsRequest { To = phone, Message = message });
}
```

## Targets

`net8.0` and `netstandard2.0` (covers .NET Framework 4.6.1+, .NET Core, .NET 5+).

## Building locally

```bash
dotnet build -c Release
dotnet pack -c Release
```

Produces `bin/Release/SendAfrica.1.0.0.nupkg` and the matching `.snupkg`.

## Design notes

- A single `HttpClient` is reused for all requests (never created per-call),
  with a constructor overload that accepts an externally managed
  `HttpClient` for `IHttpClientFactory` scenarios.
- Full NuGet packaging metadata is set in `SendAfrica.csproj` (MIT license,
  tags, project/repo URLs, XML docs, symbol package).
