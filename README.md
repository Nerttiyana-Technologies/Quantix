<div align="center">

<img src="header.png" alt=",Quantix is a clean-room, MIT-licensed mediator" />

</div>

> A source-generated, AOT-friendly mediator for .NET. Zero runtime reflection, linker-safe, fast.

Quantix is a clean-room, MIT-licensed mediator. It moves every decision a reflection-based
mediator makes at *runtime* — handler discovery, generic closing, dispatch — to *compile time*,
using a Roslyn incremental source generator. The result: no startup assembly scan, no per-call
reflection, no trim or Native AOT warnings, and a missing handler reported as a build error
rather than a runtime exception.

## Why Quantix

- **Reflection-free.** Handlers, behaviors and dispatch are resolved and emitted at compile
  time. Nothing scans assemblies; nothing closes generics or invokes methods by reflection.
- **Native AOT and trimming clean.** Generated code references every handler by its concrete
  type, so it publishes with no trim or AOT warnings.
- **Errors at build time.** A missing handler, a duplicate handler, an ambiguous result type —
  all are `QTX` build diagnostics, not production surprises.
- **Pay for what you use.** A message with no pipeline behaviors dispatches straight to its
  handler, with no chain allocation.
- **One package, zero config.** Install `Quantix`, call `AddQuantix()`, send messages.

## Install

```sh
dotnet add package Quantix
```

Targets `net8.0` and `net10.0`.

## Quickstart

Define a message and its handler:

```csharp
using Quantix;

public sealed record GetWeather(string City) : IQuery<string>;

public sealed class GetWeatherHandler : IQueryHandler<GetWeather, string>
{
    public ValueTask<string> Handle(GetWeather query, CancellationToken ct)
        => new($"Sunny in {query.City}");
}
```

Register Quantix — one line, no assembly scan:

```csharp
builder.Services.AddQuantix();
```

Dispatch through `IMediator`:

```csharp
app.MapGet("/weather/{city}", (string city, IMediator mediator, CancellationToken ct)
    => mediator.Send(new GetWeather(city), ct));
```

That is the whole API surface for the common case. There are no marker-interface registrations,
no assembly lists, no open-generic registration ceremony.

## What you get

- **Commands, queries, notifications and streams** — `ICommand` / `ICommand<T>`, `IQuery<T>`,
  `INotification`, `IStreamRequest<T>`, dispatched by `Send`, `Publish` and `Stream`.
- **Pipeline behaviors** — closed or open-generic. An open-generic behavior is scoped by its
  own generic constraints: `where TRequest : ICommand<TResult>` wraps commands and nothing else.
- **Generic messages** — a message type may itself be generic; Quantix discovers each concrete
  instantiation used in the program and emits a closed dispatch path for it.
- **Adaptive dispatch** — a type-pattern chain for small message sets, a frozen-dictionary jump
  table for large ones; the switch point is benchmark-tuned.
- **Compile-time diagnostics** — the `QTX` diagnostic catalogue turns missing or ambiguous
  handlers into build errors.
- **Navigation hints** — an analyzer names the handler that runs at each `Send` / `Publish` /
  `Stream` call site.
- **An opt-in pipeline manifest** — set `<QuantixEmitManifest>true</QuantixEmitManifest>` to
  generate a human-readable map of every message, its handler and its behavior chain.

## How it works

Quantix ships as a single package containing the public abstractions and a Roslyn incremental
source generator. At build time the generator discovers every handler and behavior, validates
them, and emits two things into your assembly: the `QuantixMediator` dispatcher and the
`AddQuantix()` dependency-injection registration. The dependency-injection container only ever
sees closed types, which is what keeps Quantix AOT-clean.

## Requirements

- A consuming project on `net8.0` or `net10.0`.
- A C#-capable build (the generator targets `netstandard2.0`, so it loads in every modern SDK
  and IDE).

## License

[MIT](LICENSE)
