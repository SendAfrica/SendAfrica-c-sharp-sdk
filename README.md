# SendAfrica C# SDK

This is the official C# client library for the SendAfrica API. SendAfrica
provides SMS messaging and mobile-money/payment services for businesses
operating in Tanzania and East Africa. This package lets a .NET application
send SMS messages, check credit balances, top up credits, and (once the
feature ships server-side) receive webhook events, without having to write
raw HTTP calls by hand.

If you've used the
[official Python SDK](https://github.com/SendAfrica/SendAfrica-python-sdk),
this library follows the same structure and naming on purpose, so knowledge
transfers directly between the two.

## Table of contents

- [Current status](#current-status)
- [Requirements](#requirements)
- [Installing the package](#installing-the-package)
- [Getting an API key](#getting-an-api-key)
- [Quickstart](#quickstart)
- [Configuring the client](#configuring-the-client)
- [Sending SMS](#sending-sms)
- [Checking credits](#checking-credits)
- [Topping up credits (payments)](#topping-up-credits-payments)
- [Webhooks](#webhooks)
- [Handling errors](#handling-errors)
- [Using this SDK in an ASP.NET application](#using-this-sdk-in-an-aspnet-application)
- [Supported .NET versions](#supported-net-versions)
- [Building and testing locally](#building-and-testing-locally)
- [How the API details in this SDK were verified](#how-the-api-details-in-this-sdk-were-verified)
- [Frequently asked questions](#frequently-asked-questions)
- [Design decisions, explained](#design-decisions-explained)
- [Project layout](#project-layout)
- [Contributing](#contributing)

## Current status

Not every part of this SDK has been checked against the real SendAfrica API
in the same way. Read this section before you rely on anything beyond
sending an SMS.

**SMS sending has been tested against the live API and works.** The exact
web address it calls, how it proves who you are (authentication), and the
shape of the data it sends and receives were all checked directly with a
real account and a real API key. You can trust this part today.

**Credit balance checking, credit top-ups, and webhooks have not been
tested against the live API by this project.** Instead, the code for these
features was copied over from the official Python SDK, which is written and
maintained by the same team that builds the SendAfrica API. That is a good,
trustworthy source to copy from, but it is not the same thing as this C#
project having actually called those endpoints and confirmed they behave as
expected. Treat these three features as "should work, but please verify
before you depend on them in a real application." See
[HANDOFF.md](HANDOFF.md) for the exact list of what still needs checking.

**This package is not yet published to nuget.org.** The instructions below
under [Installing the package](#installing-the-package) describe how it will
be installed once it is published. Until then, if you want to use it today,
see [Building and testing locally](#building-and-testing-locally) for how to
build it yourself and reference it from a local folder.

## Requirements

- .NET 8.0, or any runtime that supports .NET Standard 2.0. In practice
  that covers .NET Framework 4.6.1 and later, .NET Core, and every version
  of modern .NET (5 and up). If you are not sure what this means for your
  project, it almost certainly works — .NET Standard 2.0 is a very widely
  supported baseline.
- A SendAfrica account and an API key. See
  [Getting an API key](#getting-an-api-key) below if you do not have one.

## Installing the package

Once this package is published to nuget.org, you will be able to install it
with the .NET command-line tool:

```bash
dotnet add package SendAfrica
```

Or, if you prefer working inside Visual Studio, use the graphical package
manager instead:

1. Right-click your project in Solution Explorer.
2. Choose "Manage NuGet Packages".
3. Search for "SendAfrica".
4. Click "Install".

As noted above in [Current status](#current-status), this package has not
been published yet, so neither of these will work today. Until it is
published, use the local build steps in
[Building and testing locally](#building-and-testing-locally) instead.

## Getting an API key

You will need an API key from your SendAfrica account before you can send
any messages. Log in to your SendAfrica dashboard, find the "API Keys"
section, and create a new key. SendAfrica shows you the full key value only
once, immediately after you create it — copy it somewhere safe right away,
because it cannot be viewed again afterward. If you lose it, you will need
to revoke that key and create a new one.

Keep this key secret. Do not commit it to source control, and do not paste
it into chat messages, screenshots, or anywhere else it might be logged or
seen by someone else. Treat it the same way you would treat a password.

## Quickstart

Here is the smallest possible example of sending a message:

```csharp
using SendAfrica;

var client = new SendAfricaClient("SA-your-api-key-here");

var result = await client.Sms.SendAsync("0712345678", "Welcome to SendAfrica");

Console.WriteLine($"{result.MessageId} ({result.Status}, {result.CreditsUsed} credit(s) used)");
```

Walking through what happens here:

1. `new SendAfricaClient("SA-your-api-key-here")` creates a client object.
   This is the main entry point for everything the SDK does — you generally
   only need one instance of this per application, and you can reuse it for
   every request.
2. `client.Sms.SendAsync(...)` sends the actual message. The first argument
   is the recipient's phone number, and the second is the message text.
3. The `await` keyword is required because this is an asynchronous
   operation — it makes a network call over the internet, which takes time,
   and `await` lets your program do other work while it waits instead of
   freezing.
4. `result` is an `SmsResult` object containing the message ID that
   SendAfrica assigned, the delivery status, and how many credits were
   used.

Instead of passing your API key directly in code, you can set it as an
environment variable named `SENDAFRICA_API_KEY`. If you do that, you can
create the client with no arguments at all:

```csharp
// Assumes the SENDAFRICA_API_KEY environment variable is already set
var client = new SendAfricaClient();
```

This is usually the better approach for real applications, since it keeps
the key out of your source code entirely.

## Configuring the client

The `SendAfricaClient` constructor accepts several optional settings beyond
just the API key:

```csharp
var client = new SendAfricaClient(
    apiKey: "SA-your-api-key-here",                    // or leave null to use SENDAFRICA_API_KEY
    baseUrl: SendAfricaClient.DefaultBaseUrl,           // "https://api.sendafrica.online/v1"
    timeoutSeconds: SendAfricaClient.DefaultTimeoutSeconds, // 10
    maxRetries: SendAfricaClient.DefaultMaxRetries,     // 3
    environment: "production",                          // just a label, for your own logging
    debug: false,                                        // set true to print request logs to the console
    webhookSecret: null                                  // only needed if you use the Webhooks feature
);
```

Here is what each setting does:

| Setting | Default | What it controls |
|---|---|---|
| `apiKey` | reads `SENDAFRICA_API_KEY` if not given | The key that authenticates every request. Required, one way or another. |
| `baseUrl` | `https://api.sendafrica.online/v1` | The web address the SDK sends requests to. You would only change this if SendAfrica gave you a different address, for example a staging/testing environment. |
| `timeoutSeconds` | `10` | How many seconds to wait for a response before giving up on a single request attempt. |
| `maxRetries` | `3` | How many times to automatically retry a request that failed due to a temporary problem (see [Handling errors](#handling-errors) for exactly which failures are retried). |
| `environment` | `"production"` | A plain text label you can use in your own logs to tell environments apart. SendAfrica does not use this value itself. |
| `debug` | `false` | When `true`, the SDK prints a line to the console for every request it makes and the status code it got back. Useful while you are getting things working, noisy in production. |
| `webhookSecret` | `null` | A secret key used to verify that incoming webhook data really came from SendAfrica. Only relevant if you use `client.Webhooks`. |

A note for readers coming from the Python SDK: that library has a separate
`AsyncSendAfrica` class for asynchronous code, because Python needs two
different code paths for synchronous and asynchronous HTTP calls. C# does
not have that problem — every method in this SDK already returns a `Task`,
which is the standard, built-in way C# handles asynchronous work. That
means there is only one client class here, and it works the same way
whether you are writing a console app, a web API, or a background service.

## Sending SMS

The most common thing you will do with this SDK is send a text message:

```csharp
var result = await client.Sms.SendAsync(
    to: "0712345678",
    message: "Your OTP is 123456",
    sender: "MyBrand" // optional — see the note below
);

Console.WriteLine(result.MessageId);    // a unique ID SendAfrica assigned, e.g. "SA-abc123..."
Console.WriteLine(result.Status);       // delivery status, e.g. "Success"
Console.WriteLine(result.CreditsUsed);  // how many credits this message cost, usually 1
```

The `sender` parameter is optional and lets you set a custom sender name
(sometimes called a sender ID) instead of a phone number showing as the
sender. It must be 11 characters or fewer. Whether your SendAfrica account
is approved to use custom sender IDs depends on your account setup — check
with SendAfrica if this does not seem to be working, since it has not been
independently confirmed by this SDK's own testing (see
[Current status](#current-status)).

### Phone numbers can be in almost any format

You do not need to format phone numbers yourself before passing them in.
The SDK automatically converts whatever you give it into the international
E.164 format that the API expects, and it does this on your own computer,
before any data is sent over the network. Here is what that conversion
looks like:

| What you type in | What actually gets sent |
|---|---|
| `0712345678` | `+255712345678` |
| `712345678` | `+255712345678` |
| `255712345678` | `+255712345678` |
| `+255712345678` | `+255712345678` |
| `+255 712 345 678` | `+255712345678` |

The default country code is `255` (Tanzania). If you type in something that
cannot be confidently turned into a valid phone number — for example, a
string with letters in it, or a number that is far too short — the SDK
throws an `InvalidPhoneException` immediately, before it ever tries to
contact the SendAfrica servers. This means a typo in a phone number never
wastes a network call, and never risks sending a message to the wrong
place.

### Sending to many people at once

If you need to send the same or different messages to a list of recipients,
use `SendManyAsync` instead of calling `SendAsync` in a loop yourself:

```csharp
var results = await client.Sms.SendManyAsync(new[]
{
    new BulkSmsMessage { To = "0711111111", Message = "Hello John" },
    new BulkSmsMessage { To = "0722222222", Message = "Hello Mary" },
}, sender: "MyBrand");

Console.WriteLine($"{results.SentCount} sent successfully, {results.FailedCount} failed");

foreach (var failure in results.Failed)
{
    Console.WriteLine($"Could not send to {failure.To}: {failure.Error}");
}
```

Under the hood, this simply calls `SendAsync` once per message in your
list — it is not a special bulk endpoint on the server. The value it adds
is that if one message in the list fails (for example, because of an
invalid phone number), the rest of the batch still goes out. You get back a
`BulkSmsResult` with two lists: `Results` for everything that succeeded,
and `Failed` for everything that did not, along with the reason for each
failure.

By default, messages are sent at a rate of 10 per second so as not to
overwhelm the API. You can change this with the `rateLimitPerSec`
parameter if you need to go slower or faster.

### Previewing the cost before you send

Before actually sending a message, you can ask the SDK to estimate how many
credits it will cost and how it will be encoded. This calculation happens
entirely on your own computer — it does not contact SendAfrica's servers at
all, so it is instant and free to call as often as you like:

```csharp
var analysis = client.Sms.Analyze("Habari, how are you?");

Console.WriteLine(analysis.Encoding);   // "GSM-7" or "UCS-2"
Console.WriteLine(analysis.Characters); // how many characters are in the message
Console.WriteLine(analysis.Parts);      // how many SMS segments the message will be split into
Console.WriteLine(analysis.Credits);    // estimated credits (usually equal to Parts)
```

This is useful for showing a cost estimate in a user interface before
someone clicks "send". A quick explanation of the two encodings mentioned
above, since they affect both cost and message length limits:

- **GSM-7** is used for messages containing only basic Latin letters,
  numbers, and common punctuation. A single message can hold up to 160
  characters this way.
- **UCS-2** is used automatically as soon as a message contains characters
  outside that basic set — emoji are the most common example, along with
  some accented characters. Messages in this encoding are limited to 70
  characters per segment, which is why adding a single emoji to an
  otherwise-short message can sometimes unexpectedly push it into a second,
  more expensive segment.

Keep in mind that `Analyze` gives an estimate for planning purposes. The
number that actually gets charged to your account is always
`SmsResult.CreditsUsed`, returned from the real `SendAsync` call.

## Checking credits

```csharp
var balance = await client.Credits.BalanceAsync();
Console.WriteLine($"{balance.Balance} credits remaining on account {balance.AccountId}");

var history = await client.Credits.HistoryAsync(page: 1, perPage: 50);
foreach (var transaction in history)
{
    Console.WriteLine($"{transaction.Type} of {transaction.Amount} -> balance afterward: {transaction.BalanceAfter}");
}
```

`HistoryAsync` returns your account's credit transaction log — every time
credits were added (a top-up) or spent (sending a message), one entry
appears here. Results are paginated: `page` starts at 1, and `perPage`
controls how many entries come back per call (up to a maximum of 200).

As mentioned in [Current status](#current-status), this feature has not yet
been confirmed against the live SendAfrica API by this project's own
testing. It is copied from the official Python SDK, which is a reliable
source, but if something looks off when you try it, please check
[HANDOFF.md](HANDOFF.md) and consider it a candidate for the first thing to
verify.

## Topping up credits (payments)

SendAfrica's credit top-ups work on a pay-as-you-go basis: you choose any
amount in Tanzanian shillings (TZS) above a minimum threshold, and the API
converts that amount into credits using a tiered pricing schedule (larger
top-ups get a better rate per credit).

You can check the current pricing before creating a payment:

```csharp
var rate = await client.Payments.RateAsync();
Console.WriteLine($"Minimum top-up: {rate.MinAmountTzs} TZS");

foreach (var tier in rate.Tiers)
{
    Console.WriteLine($"Up to {tier.MaxAmountTzs} TZS: {tier.RateTzsPerCredit} TZS per credit");
}
```

Then create the actual top-up:

```csharp
// A manual top-up, e.g. one you will reconcile by bank transfer
var payment = await client.Payments.CreateAsync(amount: 50000);
Console.WriteLine($"{payment.Id}: {payment.Status}, expecting {payment.CreditAmount} credits");

// A mobile-money top-up requires a phone number as well
var mobileMoneyPayment = await client.Payments.CreateAsync(
    amount: 50000,
    provider: "snippe",
    phone: "0712345678"
);
```

This only wraps the endpoints that are reachable using an API key.
Looking up or listing past payments is intentionally not included here,
because that functionality is only available through an admin login, not
an API key — attempting to add it would not actually work.

As with credit balance checking, this feature has been copied from the
Python SDK's implementation but not independently confirmed against the
live API by this project — see [Current status](#current-status).

## Webhooks

This feature is speculative. As of the time this SDK was written, the
SendAfrica API does not yet send outbound notifications (webhooks) to your
own server when something happens, such as a message being delivered. This
part of the SDK is built ahead of time so that it will be ready the moment
that feature ships on SendAfrica's side — but there is nothing live to
receive from yet.

When it does become available, usage will look like this:

```csharp
// Inside your webhook endpoint handler
var evt = client.Webhooks.Parse(
    requestBody,
    signature: request.Headers["X-SendAfrica-Signature"]
);

if (evt.Type == "sms.delivered")
{
    Console.WriteLine($"Message {evt.MessageId} was delivered");
}
```

If you provide both a signature and a secret (either passed directly to
`Parse`, or configured once on the client via the `webhookSecret` setting),
the SDK verifies that the incoming data genuinely came from SendAfrica by
checking a cryptographic signature (HMAC-SHA256), before trusting any of
its contents. If verification fails, it throws a
`WebhookSignatureException` instead of returning the (possibly forged)
event, so you can safely reject the request without processing it.

## Handling errors

Every error this SDK can raise inherits from a single base class,
`SendAfricaException`. This means you have a choice: catch that one base
class if you just want to handle "something went wrong" in one place, or
catch more specific exception types if you want to react differently to
different kinds of failures.

```csharp
try
{
    await client.Sms.SendAsync("0712345678", "Hello");
}
catch (InsufficientCreditsException)
{
    // Your account is out of credits — prompt the user to top up
}
catch (RateLimitException ex)
{
    // You are sending too fast — ex.RetryAfter tells you how long to wait
}
catch (InvalidPhoneException ex)
{
    // The phone number could not be understood — ex.Message explains why
}
catch (SendAfricaException ex)
{
    // Anything else. Every exception carries extra detail you can log:
    // ex.StatusCode  — the HTTP status code, if this came from a server response
    // ex.ErrorCode   — SendAfrica's own short error code, e.g. "invalid_api_key"
    // ex.RequestId   — a unique ID for this request, useful when contacting support
}
```

Here is the full list of exception types and when each one happens:

| Exception | When it happens |
|---|---|
| `SendAfricaException` | The base class every other exception inherits from. Catch this to handle any SDK error generically. |
| `AuthenticationException` | Your API key is missing, invalid, or has been revoked (HTTP 401). |
| `ValidationException` | The data you sent was rejected as invalid by the API (HTTP 400 or 422). |
| `InvalidPhoneException` | A phone number could not be understood, either by this SDK's own local check or by the server. This is a more specific kind of `ValidationException`. |
| `InsufficientCreditsException` | Your account does not have enough credits to complete the request (HTTP 402). |
| `RateLimitException` | You have sent too many requests too quickly (HTTP 429). Has a `RetryAfter` property telling you how long to wait before trying again. |
| `NotFoundException` | You asked for something that does not exist (HTTP 404). |
| `ServerException` | Something went wrong on SendAfrica's servers, not something you did (HTTP 500 and similar). |
| `ApiConnectionException` | The request never reached SendAfrica at all — for example, no internet connection, or the request timed out. |
| `WebhookSignatureException` | An incoming webhook's signature did not match, meaning it may not genuinely be from SendAfrica. |

### Automatic retries

Some failures are temporary and likely to succeed if you simply try again a
moment later — for example, a brief server hiccup, or a rate limit that
will have cleared in a few seconds. Rather than making you write that retry
logic yourself, the SDK does it automatically for these specific
situations:

- HTTP 429 (rate limited) — waits for the amount of time the server asks
  for, if it tells you, via the `Retry-After` header.
- HTTP 500, 502, 503, 504 (server-side problems) — waits, then tries again.
- Connection failures, such as a dropped network connection.

The wait time between attempts increases each time (this is called
"exponential backoff"), so a temporary problem does not turn into a flood
of retries hitting the server all at once. By default it will try up to 3
times beyond the original attempt before giving up and raising an
exception; you can change this with the `maxRetries` setting described
under [Configuring the client](#configuring-the-client).

Errors that are not temporary — like an invalid phone number, or a missing
API key — are never retried, since trying again would just fail the same
way.

## Using this SDK in an ASP.NET application

If you are building a web application or API with ASP.NET, the recommended
approach is to let ASP.NET's built-in `IHttpClientFactory` manage the
`HttpClient` used internally by this SDK, rather than letting
`SendAfricaClient` create its own. This matters because creating a new
`HttpClient` for every request in a busy web application is a well-known
way to run into a problem called socket exhaustion, where your application
runs out of available network connections under load.

Register the client once, in your application's startup code:

```csharp
// Program.cs
builder.Services.AddHttpClient<SendAfricaClient>((httpClient, serviceProvider) =>
    new SendAfricaClient(httpClient, apiKey: builder.Configuration["SendAfrica:ApiKey"]));
```

Then you can have `SendAfricaClient` injected into any class that needs it,
the same way you would with any other ASP.NET service:

```csharp
public class NotificationService
{
    private readonly SendAfricaClient _sendAfrica;

    public NotificationService(SendAfricaClient sendAfrica)
    {
        _sendAfrica = sendAfrica;
    }

    public Task NotifyAsync(string phone, string message) =>
        _sendAfrica.Sms.SendAsync(phone, message);
}
```

## Supported .NET versions

This package targets both `net8.0` and `netstandard2.0` at the same time,
which is a common pattern for widely-used .NET libraries. In practice this
means:

- If your project uses .NET 8 or later, you get the `net8.0` build, which
  can take advantage of the newest .NET features.
- If your project uses something older — including .NET Framework, which is
  still common in many established Tanzanian and East African businesses
  running ASP.NET on Windows Server — you get the `netstandard2.0` build
  instead, which is compatible all the way back to .NET Framework 4.6.1.

You do not need to choose which one you get; NuGet figures this out
automatically based on your project.

## Building and testing locally

Since this package is not yet published to nuget.org (see
[Current status](#current-status)), here is how to use it directly from
source in the meantime.

First, clone the repository and build it:

```bash
git clone https://github.com/SendAfrica/SendAfrica-c-sharp-sdk.git
cd SendAfrica-c-sharp-sdk
dotnet build -c Release
dotnet pack -c Release
```

The second command produces `bin/Release/SendAfrica.1.0.0.nupkg` (and a
matching `.snupkg` file with debug symbols). You can then reference this
local package from another project on your machine without needing
nuget.org at all:

```bash
# From your own project's folder
dotnet nuget add source "C:\path\to\SendAfrica-c-sharp-sdk\bin\Release" -n sendafrica-local
dotnet add package SendAfrica --source "C:\path\to\SendAfrica-c-sharp-sdk\bin\Release"
```

Alternatively, for quick experimentation, you can add a direct project
reference instead of going through a package at all:

```bash
dotnet add reference "C:\path\to\SendAfrica-c-sharp-sdk\SendAfrica.csproj"
```

## How the API details in this SDK were verified

This section explains exactly what "verified" and "not yet verified" mean
in [Current status](#current-status), for anyone who wants the full detail.

The SMS feature was checked by making real requests directly to
`api.sendafrica.online` using a genuine account API key, and observing
exactly what came back — trying a key that did not exist, trying the real
key, trying a few different possible web addresses, trying both ways of
sending the API key, and sending requests with fields deliberately missing.
This confirmed, with certainty rather than guesswork, the exact web address
used (`POST /v1/sms`), both accepted ways of authenticating
(`Authorization: Bearer <key>` and `X-API-Key: <key>`), and the exact shape
of the data sent back for both successful and failed requests. All of that
is what the SMS-sending code in this SDK now reflects.

One side effect of that process is worth being transparent about: two test
messages were accidentally delivered to a real phone number during this
verification, using a phone number that looked fake but happened to be a
validly-formatted one, which used up 2 real account credits. The lesson
from that mistake, and the safe way to do this kind of testing going
forward, is written up in [HANDOFF.md](HANDOFF.md).

The credit balance, payments, and webhook features were not checked this
same way. Instead, their code was copied over from the official Python
SDK's own implementation of those same features, which was written by
people with direct access to SendAfrica's backend code — a trustworthy
source, but a different kind of confidence than this project having made
its own real requests and watched the real responses come back, the way it
did for SMS.

## Frequently asked questions

**Why do I get an `AuthenticationException` right away, before any of my
code even seems to run?**
This almost always means no API key was found. Either pass one explicitly
to `new SendAfricaClient("your-key")`, or make sure the
`SENDAFRICA_API_KEY` environment variable is actually set in the
environment your application is running in — a variable set in one
terminal window will not automatically be visible to an application
launched a different way.

**Why does `dotnet add package SendAfrica` say the package cannot be
found?**
Because it has not been published to nuget.org yet — see
[Current status](#current-status) and
[Building and testing locally](#building-and-testing-locally) for how to
use it in the meantime.

**Can I use this SDK from a synchronous, non-async codebase?**
Technically yes, by calling `.Result` or `.GetAwaiter().GetResult()` on the
returned `Task`, but this is not recommended — it can cause your
application to freeze (deadlock) in some contexts, particularly in older
ASP.NET applications. If at all possible, make the calling method `async`
and use `await` normally instead.

**Does this SDK retry my request automatically if it fails?**
Only for specific, temporary failure types — see
[Automatic retries](#automatic-retries) above. Errors caused by something
in your own request, like an invalid phone number or a missing field, are
never retried, since retrying would just produce the same failure again.

**Is it safe to call `Analyze` or `RateAsync` as often as I want?**
`Analyze` is completely local and free — it never contacts SendAfrica at
all. `RateAsync` and `BalanceAsync` do make a network call, but they are
both read-only `GET` requests with no side effects and no credit cost, so
calling them frequently (for example, before every send, to check your
balance) is safe.

## Design decisions, explained

A few choices in how this SDK is built are worth explaining, in case you
are curious why something works the way it does, or if you are considering
contributing changes.

**One shared `HttpClient`, not a new one per request.** Creating a fresh
`HttpClient` object for every single network call is a common mistake in
.NET applications, and it can exhaust the operating system's available
network connections under sustained load. This SDK creates one `HttpClient`
when you create a `SendAfricaClient`, and reuses it for every request made
through that client for as long as it exists.

**Web addresses are built by joining plain text, not using `HttpClient`'s
built-in address-combining feature.** This sounds like a strange thing to
call out, but it avoids a genuinely surprising bug: .NET's built-in way of
combining a "base address" with a relative path has a rule where, if the
relative path starts with a forward slash, it replaces the entire path
instead of adding onto it. Concretely, combining a base address of
`https://host/v1/` with a path of `/sms` using .NET's built-in method
produces `https://host/sms` — silently dropping the `/v1` part, which would
have sent every request to the wrong address without any obvious error.
This SDK avoids that entirely by building the full web address as a single
piece of text up front.

**Every response is deserialized through a shared "envelope" type.** Every
response SendAfrica's API sends back — success or failure — is wrapped in a
consistent outer structure containing a `success` flag, the actual data (on
success) or error details (on failure), and a timestamp. Rather than
duplicating that unwrapping logic in every single method, it lives in one
place (`SendAfricaTransport.cs`) that every resource class relies on.

## Project layout

```
SendAfrica-c-sharp-sdk/
├── SendAfrica.csproj       # Package metadata, target frameworks
├── SendAfricaClient.cs     # The main entry point — SendAfricaClient class
├── SendAfricaTransport.cs  # Internal: HTTP calls, retries, error mapping
├── Exceptions.cs           # The full exception hierarchy
├── Models.cs                # Response types: SmsResult, CreditBalance, Payment, etc.
├── Resources/
│   ├── SmsResource.cs       # client.Sms.*
│   ├── CreditsResource.cs   # client.Credits.*
│   ├── PaymentsResource.cs  # client.Payments.*
│   └── WebhooksResource.cs  # client.Webhooks.*
├── Utils/
│   ├── PhoneUtil.cs         # Phone number normalization
│   ├── SmsAnalyzer.cs       # Message encoding/cost estimation
│   └── Validators.cs        # Small shared input checks
├── README.md                 # This file
└── HANDOFF.md                # Detailed status of what is and is not confirmed
```

## Contributing

If you are extending this SDK — for example, adding a new endpoint — a
useful pattern to follow is the one already used throughout: add any new
response type to `Models.cs`, add a resource class under `Resources/` that
takes the internal transport in its constructor and calls
`_transport.RequestAsync<T>(...)`, then wire it up as a new property on
`SendAfricaClient`. Check [HANDOFF.md](HANDOFF.md) first for known
open questions before assuming an endpoint's exact shape — several details
in this SDK are marked there as needing confirmation against the real API
rather than being fully settled.
