using Microsoft.Extensions.DependencyInjection;
using Quantix;
using Xunit;

namespace Quantix.IntegrationTests;

/// <summary>Integration tests for the generated pipeline behavior chain.</summary>
public class BehaviorTests
{
    [Fact]
    public async Task Behaviors_wrap_the_handler_lowest_order_outermost()
    {
        var log = new List<string>();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(log)
            .AddQuantix()
            .BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new TracedRequest());

        Assert.Equal(
            new[] { "outer:before", "inner:before", "handler", "inner:after", "outer:after" },
            log);
    }
}

/// <summary>A command wrapped by two pipeline behaviors.</summary>
public sealed record TracedRequest : ICommand<int>;

/// <summary>Handles <see cref="TracedRequest"/>.</summary>
/// <param name="log">The shared execution log.</param>
public sealed class TracedRequestHandler(List<string> log) : ICommandHandler<TracedRequest, int>
{
    /// <inheritdoc />
    public ValueTask<int> Handle(TracedRequest command, CancellationToken ct)
    {
        log.Add("handler");
        return new(0);
    }
}

/// <summary>The outermost behavior — lower <c>[PipelineOrder]</c> runs first.</summary>
/// <param name="log">The shared execution log.</param>
[PipelineOrder(10)]
public sealed class OuterBehavior(List<string> log) : IPipelineBehavior<TracedRequest, int>
{
    /// <inheritdoc />
    public async ValueTask<int> Handle(TracedRequest request, RequestHandlerDelegate<int> next, CancellationToken ct)
    {
        log.Add("outer:before");
        int result = await next(ct).ConfigureAwait(false);
        log.Add("outer:after");
        return result;
    }
}

/// <summary>The innermost behavior — higher <c>[PipelineOrder]</c> runs closer to the handler.</summary>
/// <param name="log">The shared execution log.</param>
[PipelineOrder(20)]
public sealed class InnerBehavior(List<string> log) : IPipelineBehavior<TracedRequest, int>
{
    /// <inheritdoc />
    public async ValueTask<int> Handle(TracedRequest request, RequestHandlerDelegate<int> next, CancellationToken ct)
    {
        log.Add("inner:before");
        int result = await next(ct).ConfigureAwait(false);
        log.Add("inner:after");
        return result;
    }
}
