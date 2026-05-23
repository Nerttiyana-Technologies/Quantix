using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MediatrMediator = MediatR.IMediator;
using QuantixMediator = Quantix.IMediator;

namespace Quantix.Benchmarks;

/// <summary>
/// Registration / cold-start cost — Quantix versus the MediatR baseline (plan L5-7). Each
/// benchmark registers the mediator and builds the container from scratch. Quantix registers a
/// generated, closed set of services with no assembly scan; MediatR scans the assembly.
/// </summary>
[MemoryDiagnoser]
public class ColdStartBenchmarks
{
    /// <summary>The MediatR baseline: scan the assembly, register, and build the container.</summary>
    [Benchmark(Baseline = true)]
    public MediatrMediator MediatR_Register()
        => new ServiceCollection()
            .AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatrCommand).Assembly))
            .BuildServiceProvider()
            .GetRequiredService<MediatrMediator>();

    /// <summary>Quantix: register the generated, closed service set and build the container.</summary>
    [Benchmark]
    public QuantixMediator Quantix_Register()
        => new ServiceCollection()
            .AddQuantix()
            .BuildServiceProvider()
            .GetRequiredService<QuantixMediator>();
}
