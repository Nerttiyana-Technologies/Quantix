using Microsoft.Extensions.DependencyInjection;
using Quantix;
using Xunit;

namespace Quantix.IntegrationTests;

/// <summary>Integration tests for <see cref="IMediator"/>.Publish through the generated dispatcher.</summary>
public class PublishTests
{
    [Fact]
    public async Task Publish_invokes_every_notification_handler_in_order()
    {
        var log = new List<string>();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(log)
            .AddQuantix()
            .BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new ThingHappened());

        Assert.Equal(new[] { "alpha", "beta" }, log);
    }

    [Fact]
    public async Task Publish_runs_handlers_in_NotificationOrder()
    {
        var log = new List<string>();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(log)
            .AddQuantix()
            .BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new Staged());

        Assert.Equal(new[] { "1", "2" }, log);
    }
}

/// <summary>A notification handled by two handlers.</summary>
public sealed record ThingHappened : INotification;

/// <summary>The first handler of <see cref="ThingHappened"/>.</summary>
/// <param name="log">The shared log the handlers append to.</param>
public sealed class AlphaHandler(List<string> log) : INotificationHandler<ThingHappened>
{
    /// <inheritdoc />
    public ValueTask Handle(ThingHappened notification, CancellationToken ct)
    {
        log.Add("alpha");
        return default;
    }
}

/// <summary>The second handler of <see cref="ThingHappened"/>.</summary>
/// <param name="log">The shared log the handlers append to.</param>
public sealed class BetaHandler(List<string> log) : INotificationHandler<ThingHappened>
{
    /// <inheritdoc />
    public ValueTask Handle(ThingHappened notification, CancellationToken ct)
    {
        log.Add("beta");
        return default;
    }
}

/// <summary>A notification whose handlers are explicitly ordered with <c>[NotificationOrder]</c>.</summary>
public sealed record Staged : INotification;

/// <summary>Runs first by <c>[NotificationOrder]</c>, despite a later type name.</summary>
/// <param name="log">The shared execution log.</param>
[NotificationOrder(1)]
public sealed class ZebraHandler(List<string> log) : INotificationHandler<Staged>
{
    /// <inheritdoc />
    public ValueTask Handle(Staged notification, CancellationToken ct)
    {
        log.Add("1");
        return default;
    }
}

/// <summary>Runs second by <c>[NotificationOrder]</c>, despite an earlier type name.</summary>
/// <param name="log">The shared execution log.</param>
[NotificationOrder(2)]
public sealed class AntelopeHandler(List<string> log) : INotificationHandler<Staged>
{
    /// <inheritdoc />
    public ValueTask Handle(Staged notification, CancellationToken ct)
    {
        log.Add("2");
        return default;
    }
}
