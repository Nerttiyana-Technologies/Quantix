# Changelog

All notable changes to **Quantix** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Package versions are derived from git tags by [MinVer](https://github.com/adamralph/minver):
tagging a commit `v1.0.0` produces package version `1.0.0`.

## [Unreleased]

_Nothing yet._

## [1.0.1] - 2026-05-26

### Fixed

- The generated dispatcher emitted an illegal direct cast in the frozen
  (large-message-set) routing path, breaking compilation for consumers whose
  generic-interface messages (`ICommand<TResult>`, `IQuery<TResult>`,
  `IStreamRequest<TResult>`) were declared `sealed` — the standard C# message
  shape. The discriminator already proves the runtime type at that point, so
  the cast now goes through `object` (zero cost on reference types, identical
  IL). Pattern-chain routing (used below the message-count threshold) was
  unaffected. A regression test in `AdaptiveDispatchTests` now compiles the
  emitted source for the frozen path so this class of mistake cannot recur.

## [1.0.0] - 2026-05-23

The first release of Quantix — a source-generated, AOT-friendly mediator for .NET.
Handler discovery, generic closing and dispatch all happen at compile time, so there
is no startup scan, no per-call reflection, and no trim or AOT warnings.

### Added

- Source-generated mediator: an incremental Roslyn generator emits the dispatcher and
  the dependency-injection registration into the consuming assembly.
- Message kinds: void commands, result commands, queries, notifications and stream
  requests — `ICommand`, `ICommand<TResult>`, `IQuery<TResult>`, `INotification`,
  `IStreamRequest<TResult>`.
- `IMediator` with `Send`, `Publish` and `Stream`.
- Pipeline behaviors, including constraint-aware open-generic behaviors, ordered with
  `[PipelineOrder]`.
- Notification handlers run sequentially, ordered with `[NotificationOrder]`.
- Generic message types, discovered from their construction sites and closed at
  compile time.
- Adaptive dispatch: a type-pattern chain for small message sets and a
  `FrozenDictionary` jump-table above a benchmark-tuned threshold.
- Twelve compile-time diagnostics, `QTX0001`–`QTX0012`, that turn missing handlers,
  duplicate handlers and signature mismatches into build errors.
- Navigation analyzer: `QTX0012` names the handler at each `Send` / `Publish` /
  `Stream` call site.
- A single NuGet package, `Quantix`, bundling the abstractions and the generator.
- Native AOT and trimming support, with zero trim or AOT warnings.

[Unreleased]: https://github.com/isureshsubramanian/Quantix/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/isureshsubramanian/Quantix/releases/tag/v1.0.1
[1.0.0]: https://github.com/isureshsubramanian/Quantix/releases/tag/v1.0.0
