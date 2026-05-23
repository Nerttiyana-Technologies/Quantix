# Contributing to Quantix

Thanks for your interest in Quantix — a source-generated, AOT-friendly mediator for
.NET. This guide covers how to build, test and contribute to the project.

## Prerequisites

- The .NET SDK pinned in [`global.json`](global.json) — .NET 10.0.300, or a later
  10.0 feature band. Install it from <https://dotnet.microsoft.com/download>.
- To Native-AOT-publish the sample locally you also need the platform C toolchain
  (on Linux: `clang` and the `zlib` development headers).

## Build, test, pack

Quantix is a single solution, `Quantix.slnx`:

    dotnet restore Quantix.slnx
    dotnet build   Quantix.slnx -c Release
    dotnet test    Quantix.slnx -c Release
    dotnet pack    src/Quantix/Quantix.csproj -c Release

`dotnet pack` produces the single `Quantix` package: the generator under
`analyzers/dotnet/cs/` and the abstractions under `lib/`.

To check the reflection-free guarantee, Native-AOT-publish the sample — it must
produce **zero** trim or AOT warnings (the build treats warnings as errors):

    dotnet publish samples/Quantix.Sample.MinimalApi -c Release

Add `-r <runtime-identifier>` (for example `-r osx-arm64` or `-r linux-x64`) to
target a specific platform.

## Project layout

| Path | What it is |
|---|---|
| `src/Quantix.Abstractions` | The public interfaces and attributes consumers implement. |
| `src/Quantix.Generator` | The Roslyn incremental generator, navigation analyzer and diagnostics. |
| `src/Quantix` | The packaging project — bundles the two above into one NuGet package. |
| `tests/Quantix.Generator.Tests` | Drives the generator and asserts on emitted code and diagnostics. |
| `tests/Quantix.IntegrationTests` | Runs real messages through a real dependency-injection container. |
| `benchmarks/` | The BenchmarkDotNet suite, measured against MediatR. |
| `samples/` | A Minimal API reference application. |

## Coding standards

- The build runs with `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` — a
  warning fails the build. Run `dotnet build` before opening a pull request.
- Code style is enforced by [`.editorconfig`](.editorconfig); most IDEs apply it
  automatically.
- The generator targets `netstandard2.0` and is compiled against Roslyn 4.8.0. Do
  not call Roslyn APIs newer than 4.8.0 — they will not load in older IDEs and
  SDKs, and CI's Roslyn-floor leg will fail.
- Keep public API changes to `Quantix.Abstractions` deliberate, and record them in
  `CHANGELOG.md`.

## Diagnostics

Compile-time diagnostics use the `QTX` prefix, numbered sequentially (`QTX0001`,
`QTX0002`, …). A new diagnostic takes the next free number, a descriptor in
`QuantixDiagnostics.cs`, and a test in `tests/Quantix.Generator.Tests`.

## Pull requests

- Keep each pull request focused on a single concern.
- Add or update tests for any behavior change: generator changes need an emission or
  diagnostic test; runtime changes need an integration test.
- Record user-visible changes in `CHANGELOG.md` under the `Unreleased` heading.
- Make sure `dotnet build` and `dotnet test` are green before requesting review — CI
  runs the same steps, plus a Native-AOT publish and a Roslyn-floor build.

## Releases

Releases are cut by maintainers. Versioning is derived from git tags by
[MinVer](https://github.com/adamralph/minver): a tag `v1.2.3` produces package
version `1.2.3`. Pushing a `v*` tag triggers the release workflow, which packs and
publishes `Quantix` to NuGet and creates a GitHub release.
