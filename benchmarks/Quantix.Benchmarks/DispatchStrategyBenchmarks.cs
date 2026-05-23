using BenchmarkDotNet.Attributes;

namespace Quantix.Benchmarks;

/// <summary>
/// The adaptive-dispatch threshold benchmark (plan L5-6, design section 6.3). The generator
/// routes messages with a type-pattern chain below a message-count threshold and a
/// frozen-dictionary jump table above it. This benchmark measures both strategies across a
/// range of message counts; the crossover point is the data behind the
/// <c>FrozenDispatchThreshold</c> constant in <c>MediatorEmitter</c>.
/// </summary>
/// <remarks>
/// The message types and dispatchers come from <c>Generated/DispatchFixture.g.cs</c>, produced
/// by <c>benchmarks/generate-dispatch-fixture.py</c>. The dispatched message is mid-chain, so
/// the type-pattern figure reflects the average-case lookup rather than the worst case.
/// </remarks>
[MemoryDiagnoser]
public class DispatchStrategyBenchmarks
{
    /// <summary>The message-count sizes measured — must match the script's <c>SIZES</c>.</summary>
    [Params(4, 8, 16, 32, 64, 128)]
    public int MessageCount { get; set; }

    private Func<object, int> _typePattern = null!;
    private Func<object, int> _frozen = null!;
    private object _message = null!;

    /// <summary>Selects the two dispatchers and a representative message for the current size.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _typePattern = DispatchFixture.TypePatternDispatcher(MessageCount);
        _frozen = DispatchFixture.FrozenDispatcher(MessageCount);
        _message = DispatchFixture.RepresentativeMessage(MessageCount);
    }

    /// <summary>Routes one message through the type-pattern chain.</summary>
    [Benchmark(Baseline = true)]
    public int TypePattern() => _typePattern(_message);

    /// <summary>Routes one message through the frozen-dictionary jump table.</summary>
    [Benchmark]
    public int FrozenDictionary() => _frozen(_message);
}
