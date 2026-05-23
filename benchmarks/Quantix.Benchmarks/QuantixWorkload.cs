// The Quantix messages, handlers and behaviors the benchmark suite dispatches. Discovered by
// the Quantix generator, which emits the dispatcher and AddQuantix into this assembly.

using System.Runtime.CompilerServices;
using Quantix;

namespace Quantix.Benchmarks;

/// <summary>A command with no pipeline behaviors — the raw-dispatch baseline.</summary>
/// <param name="Value">The value the handler echoes back.</param>
public sealed record BenchCommand(int Value) : ICommand<int>;

/// <summary>Handles <see cref="BenchCommand"/>.</summary>
public sealed class BenchCommandHandler : ICommandHandler<BenchCommand, int>
{
    /// <inheritdoc />
    public ValueTask<int> Handle(BenchCommand command, CancellationToken ct) => new(command.Value);
}

/// <summary>A query with no pipeline behaviors.</summary>
/// <param name="Value">The value the handler echoes back.</param>
public sealed record BenchQuery(int Value) : IQuery<int>;

/// <summary>Handles <see cref="BenchQuery"/>.</summary>
public sealed class BenchQueryHandler : IQueryHandler<BenchQuery, int>
{
    /// <inheritdoc />
    public ValueTask<int> Handle(BenchQuery query, CancellationToken ct) => new(query.Value);
}

/// <summary>A notification dispatched to four handlers.</summary>
public sealed record BenchNotification : INotification;

/// <summary>The first handler of <see cref="BenchNotification"/>.</summary>
public sealed class BenchNotificationHandlerA : INotificationHandler<BenchNotification>
{
    /// <inheritdoc />
    public ValueTask Handle(BenchNotification notification, CancellationToken ct) => default;
}

/// <summary>The second handler of <see cref="BenchNotification"/>.</summary>
public sealed class BenchNotificationHandlerB : INotificationHandler<BenchNotification>
{
    /// <inheritdoc />
    public ValueTask Handle(BenchNotification notification, CancellationToken ct) => default;
}

/// <summary>The third handler of <see cref="BenchNotification"/>.</summary>
public sealed class BenchNotificationHandlerC : INotificationHandler<BenchNotification>
{
    /// <inheritdoc />
    public ValueTask Handle(BenchNotification notification, CancellationToken ct) => default;
}

/// <summary>The fourth handler of <see cref="BenchNotification"/>.</summary>
public sealed class BenchNotificationHandlerD : INotificationHandler<BenchNotification>
{
    /// <inheritdoc />
    public ValueTask Handle(BenchNotification notification, CancellationToken ct) => default;
}

/// <summary>A stream request that yields a fixed number of integers.</summary>
/// <param name="Count">The number of integers to yield.</param>
public sealed record BenchStream(int Count) : IStreamRequest<int>;

/// <summary>Handles <see cref="BenchStream"/>.</summary>
public sealed class BenchStreamHandler : IStreamRequestHandler<BenchStream, int>
{
    /// <inheritdoc />
    public async IAsyncEnumerable<int> Handle(BenchStream request, [EnumeratorCancellation] CancellationToken ct)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        for (int i = 0; i < request.Count; i++)
        {
            yield return i;
        }
    }
}

/// <summary>A command wrapped by three pipeline behaviors — the pay-for-play comparison point.</summary>
/// <param name="Value">The value the handler echoes back.</param>
public sealed record WrappedCommand(int Value) : ICommand<int>;

/// <summary>Handles <see cref="WrappedCommand"/>.</summary>
public sealed class WrappedCommandHandler : ICommandHandler<WrappedCommand, int>
{
    /// <inheritdoc />
    public ValueTask<int> Handle(WrappedCommand command, CancellationToken ct) => new(command.Value);
}

/// <summary>The outermost behavior wrapping <see cref="WrappedCommand"/>.</summary>
[PipelineOrder(1)]
public sealed class WrappedBehavior1 : IPipelineBehavior<WrappedCommand, int>
{
    /// <inheritdoc />
    public ValueTask<int> Handle(WrappedCommand request, RequestHandlerDelegate<int> next, CancellationToken ct)
        => next(ct);
}

/// <summary>The middle behavior wrapping <see cref="WrappedCommand"/>.</summary>
[PipelineOrder(2)]
public sealed class WrappedBehavior2 : IPipelineBehavior<WrappedCommand, int>
{
    /// <inheritdoc />
    public ValueTask<int> Handle(WrappedCommand request, RequestHandlerDelegate<int> next, CancellationToken ct)
        => next(ct);
}

/// <summary>The innermost behavior wrapping <see cref="WrappedCommand"/>.</summary>
[PipelineOrder(3)]
public sealed class WrappedBehavior3 : IPipelineBehavior<WrappedCommand, int>
{
    /// <inheritdoc />
    public ValueTask<int> Handle(WrappedCommand request, RequestHandlerDelegate<int> next, CancellationToken ct)
        => next(ct);
}
