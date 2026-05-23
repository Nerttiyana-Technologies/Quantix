using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MediatrMediator = MediatR.IMediator;
using QuantixMediator = Quantix.IMediator;

namespace Quantix.Benchmarks;

/// <summary>
/// Send throughput and allocations — Quantix versus the MediatR baseline (plan L5-2, L5-5).
/// Both mediators dispatch a behavior-free command and query, so this isolates raw dispatch.
/// </summary>
[MemoryDiagnoser]
public class SendBenchmarks
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

    /// <summary>The MediatR baseline: send a command.</summary>
    [Benchmark(Baseline = true)]
    public Task<int> MediatR_SendCommand() => _mediatr.Send(new MediatrCommand(1));

    /// <summary>Quantix: send a command.</summary>
    [Benchmark]
    public ValueTask<int> Quantix_SendCommand() => _quantix.Send(new BenchCommand(1));

    /// <summary>The MediatR baseline: send a query.</summary>
    [Benchmark]
    public Task<int> MediatR_SendQuery() => _mediatr.Send(new MediatrQuery(1));

    /// <summary>Quantix: send a query.</summary>
    [Benchmark]
    public ValueTask<int> Quantix_SendQuery() => _quantix.Send(new BenchQuery(1));
}
