# SendAfrica

Official C# SDK for the SendAfrica SMS & Payments API.

> **Status: scaffold.** The API contract used here (base URL, endpoint path,
> request/response field names, auth header) is a placeholder pending the real
> SendAfrica API docs. Every assumption is marked with a `// TODO(API):`
> comment — see [HANDOFF.md](HANDOFF.md) for the full checklist.

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
    To = "0712345678",
    Message = "Hello from SendAfrica"
});

Console.WriteLine($"{result.MessageId}: {result.Status}");
```

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
