using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MediatrMediator = MediatR.IMediator;
using QuantixMediator = Quantix.IMediator;

namespace Quantix.Benchmarks;

/// <summary>
/// Publish throughput and allocations — Quantix versus the MediatR baseline (plan L5-3).
/// Both notifications fan out to four handlers, each awaited in turn.
/// </summary>
[MemoryDiagnoser]
public class PublishBenchmarks
{
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

    /// <summary>The MediatR baseline: publish a notification to four handlers.</summary>
    [Benchmark(Baseline = true)]
    public Task MediatR_Publish() => _mediatr.Publish(new MediatrNotification());

    /// <summary>Quantix: publish a notification to four handlers.</summary>
    [Benchmark]
    public ValueTask Quantix_Publish() => _quantix.Publish(new BenchNotification());
}
