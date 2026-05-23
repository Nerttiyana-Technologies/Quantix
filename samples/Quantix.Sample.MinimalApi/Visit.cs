using Quantix;

namespace Quantix.Sample;

/// <summary>A command that records a page visit and returns the running visit count.</summary>
/// <param name="Page">The page that was visited.</param>
public sealed record RecordVisit(string Page) : ICommand<int>;

/// <summary>Handles <see cref="RecordVisit"/> — increments and returns an in-memory counter.</summary>
public sealed class RecordVisitHandler : ICommandHandler<RecordVisit, int>
{
    private static int _count;

    /// <inheritdoc />
    public ValueTask<int> Handle(RecordVisit command, CancellationToken ct)
        => new(Interlocked.Increment(ref _count));
}

/// <summary>
/// A constrained open-generic behavior that audits commands. The
/// <c>where TCommand : ICommand&lt;TResult&gt;</c> constraint scopes it to commands: Quantix
/// closes it over every command at compile time and never over a query, so the unrelated
/// <see cref="GetGreeting"/> query is left untouched. This is constraint-aware scoping
/// (design D11) — no marker interface and no manual registration.
/// </summary>
/// <typeparam name="TCommand">The command being audited.</typeparam>
/// <typeparam name="TResult">The result the command produces.</typeparam>
[PipelineOrder(20)]
public sealed class CommandAuditBehavior<TCommand, TResult> : IPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly ILogger<CommandAuditBehavior<TCommand, TResult>> _logger;

    /// <summary>Creates the behavior with a logger resolved by dependency injection.</summary>
    /// <param name="logger">The logger.</param>
    public CommandAuditBehavior(ILogger<CommandAuditBehavior<TCommand, TResult>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<TResult> Handle(
        TCommand command,
        RequestHandlerDelegate<TResult> next,
        CancellationToken ct)
    {
        _logger.LogInformation("Quantix: auditing command {Command}.", command);
        TResult result = await next(ct).ConfigureAwait(false);
        _logger.LogInformation("Quantix: audited command {Command}.", command);
        return result;
    }
}
