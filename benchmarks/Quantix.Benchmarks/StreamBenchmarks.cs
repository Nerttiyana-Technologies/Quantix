using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MediatrMediator = MediatR.IMediator;
using QuantixMediator = Quantix.IMediator;

namespace Quantix.Benchmarks;

/// <summary>
/// Stream enumeration throughput and allocations — Quantix versus the MediatR baseline
/// (plan L5-4). Each benchmark fully enumerates a stream of <see cref="ItemCount"/> items.
/// </summary>
[MemoryDiagnoser]
public class StreamBenchmarks
{
    private const int ItemCount = 100;

    private QuantixMediator _quantix = null!;
    private MediatrMediator _mediatr = null!;

    /// <summary>Builds both dependency-injection containers once before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _quantix = new ServiceCollection()
            .AddQuantix()
            .BuildServiceProvider()
            .GetRequiredService<QuantixMediator>();

        _mediatr = new ServiceCollection()
            .AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatrCommand).Assembly))
            .BuildServiceProvider()
            .GetRequiredService<MediatrMediator>();
    }

    /// <summary>The MediatR baseline: enumerate a stream to completion.</summary>
    [Benchmark(Baseline = true)]
    public async Task<int> MediatR_Stream()
    {
        int sum = 0;
        await foreach (int value in _mediatr.CreateStream(new MediatrStream(ItemCount)).ConfigureAwait(false))
        {
            sum += value;
        }

        return sum;
    }

    /// <summary>Quantix: enumerate a stream to completion.</summary>
    [Benchmark]
    public async ValueTask<int> Quantix_Stream()
    {
        int sum = 0;
        await foreach (int value in _quantix.Stream(new BenchStream(ItemCount)).ConfigureAwait(false))
        {
            sum += value;
        }

        return sum;
    }
}
