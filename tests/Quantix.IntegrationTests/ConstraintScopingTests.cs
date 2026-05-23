using Microsoft.Extensions.DependencyInjection;
using Quantix;
using Xunit;

namespace Quantix.IntegrationTests;

/// <summary>
/// Integration tests for constraint-aware scoping (design D11): a constrained open-generic
/// behavior wraps only the messages whose type arguments satisfy its generic constraints.
/// </summary>
public class ConstraintScopingTests
{
    [Fact]
    public async Task A_constrained_behavior_wraps_the_matching_message_only()
    {
        var log = new List<string>();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(log)
            .AddQuantix()
            .BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new AuditedCommand());
        await mediator.Send(new PlainQuery());

        // AuditBehavior is constrained `where TRequest : IAuditMarker`; only AuditedCommand
        // carries the marker, so Quantix closed the behavior over it and never over PlainQuery.
        Assert.Equal(new[] { "audit:AuditedCommand" }, log);
    }
}

/// <summary>Marks a message for the constrained <see cref="AuditBehavior{TRequest, TResult}"/>.</summary>
public interface IAuditMarker
{
}

/// <summary>A command that carries the audit marker.</summary>
public sealed record AuditedCommand : ICommand<int>, IAuditMarker;

/// <summary>Handles <see cref="AuditedCommand"/>.</summary>
public sealed class AuditedCommandHandler : ICommandHandler<AuditedCommand, int>
{
    /// <inheritdoc />
    public ValueTask<int> Handle(AuditedCommand command, CancellationToken ct) => new(1);
}

/// <summary>A query that does not carry the audit marker.</summary>
public sealed record PlainQuery : IQuery<int>;

/// <summary>Handles <see cref="PlainQuery"/>.</summary>
public sealed class PlainQueryHandler : IQueryHandler<PlainQuery, int>
{
    /// <inheritdoc />
    public ValueTask<int> Handle(PlainQuery query, CancellationToken ct) => new(2);
}

/// <summary>
/// A constrained open-generic behavior that records the marked messages it audits. Its
/// <c>where TRequest : IAuditMarker</c> constraint scopes it — Quantix closes it over
/// <see cref="AuditedCommand"/> and never over an unmarked message.
/// </summary>
/// <typeparam name="TRequest">The marked request being audited.</typeparam>
/// <typeparam name="TResult">The result the request produces.</typeparam>
public sealed class AuditBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : IAuditMarker
{
    private readonly List<string> _log;

    /// <summary>Creates the behavior with the shared execution log.</summary>
    /// <param name="log">The shared execution log.</param>
    public AuditBehavior(List<string> log) => _log = log;

    /// <inheritdoc />
    public ValueTask<TResult> Handle(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct)
    {
        _log.Add($"audit:{typeof(TRequest).Name}");
        return next(ct);
    }
}
