using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using QuantixMediator = Quantix.IMediator;

namespace Quantix.Benchmarks;

/// <summary>
/// Pipeline-behavior overhead (plan L5-8, design D15 pay-for-play). Compares a command with no
/// behaviors — which dispatches straight to its handler — against one wrapped by three
/// behaviors, isolating the cost the behavior chain adds.
/// </summary>
[MemoryDiagnoser]
public class BehaviorBenchmarks
{
    private QuantixMediator _quantix = null!;

    /// <summary>Builds the dependency-injection container once before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
        => _quantix = new ServiceCollection()
            .AddQuantix()
            .BuildServiceProvider()
            .GetRequiredService<QuantixMediator>();

    /// <summary>Pay-for-play: a command with no behaviors dispatches directly to its handler.</summary>
    [Benchmark(Baseline = true)]
    public ValueTask<int> NoBehaviors() => _quantix.Send(new BenchCommand(1));

    /// <summary>A command wrapped by a three-behavior pipeline chain.</summary>
    [Benchmark]
    public ValueTask<int> ThreeBehaviors() => _quantix.Send(new WrappedCommand(1));
}
