// Quantix.Benchmarks — the BenchmarkDotNet host.
//
// Discovers every [Benchmark] in the assembly. See benchmarks/README.md to run the suite.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;

// A full JSON report is exported alongside the default Markdown so a CI regression gate
// (plan L5-9 / L7-4) can diff runs. Reports land in BenchmarkDotNet.Artifacts/results.
IConfig config = ManualConfig.Create(DefaultConfig.Instance).AddExporter(JsonExporter.Full);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
