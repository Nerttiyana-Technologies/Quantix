// The MediatR messages and handlers the benchmark suite dispatches as the baseline. MediatR
// uses one IRequest marker for both commands and queries; the pairs below mirror the Quantix
// workload one-for-one so the comparison rows line up.
//
// Every MediatR interface is fully qualified: this project's namespace is Quantix.Benchmarks,
// so the enclosing Quantix namespace would otherwise shadow the same-named MediatR interfaces
// (INotification, IStreamRequest, and so on) and bind these types to Quantix by mistake.

using System.Runtime.CompilerServices;

namespace Quantix.Benchmarks;

/// <summary>The MediatR baseline for <see cref="BenchCommand"/>.</summary>
/// <param name="Value">The value the handler echoes back.</param>
public sealed record MediatrCommand(int Value) : MediatR.IRequest<int>;

/// <summary>Handles <see cref="MediatrCommand"/>.</summary>
public sealed class MediatrCommandHandler : MediatR.IRequestHandler<MediatrCommand, int>
{
    /// <inheritdoc />
    public Task<int> Handle(MediatrCommand request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}

/// <summary>The MediatR baseline for <see cref="BenchQuery"/>.</summary>
/// <param name="Value">The value the handler echoes back.</param>
public sealed record MediatrQuery(int Value) : MediatR.IRequest<int>;

/// <summary>Handles <see cref="MediatrQuery"/>.</summary>
public sealed class MediatrQueryHandler : MediatR.IRequestHandler<MediatrQuery, int>
{
    /// <inheritdoc />
    public Task<int> Handle(MediatrQuery request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}

/// <summary>The MediatR baseline for <see cref="BenchNotification"/>.</summary>
public sealed record MediatrNotification : MediatR.INotification;

/// <summary>The first handler of <see cref="MediatrNotification"/>.</summary>
public sealed class MediatrNotificationHandlerA : MediatR.INotificationHandler<MediatrNotification>
{
    /// <inheritdoc />
    public Task Handle(MediatrNotification notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>The second handler of <see cref="MediatrNotification"/>.</summary>
public sealed class MediatrNotificationHandlerB : MediatR.INotificationHandler<MediatrNotification>
{
    /// <inheritdoc />
    public Task Handle(MediatrNotification notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>The third handler of <see cref="MediatrNotification"/>.</summary>
public sealed class MediatrNotificationHandlerC : MediatR.INotificationHandler<MediatrNotification>
{
    /// <inheritdoc />
    public Task Handle(MediatrNotification notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>The fourth handler of <see cref="MediatrNotification"/>.</summary>
public sealed class MediatrNotificationHandlerD : MediatR.INotificationHandler<MediatrNotification>
{
    /// <inheritdoc />
    public Task Handle(MediatrNotification notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>The MediatR baseline for <see cref="BenchStream"/>.</summary>
/// <param name="Count">The number of integers to yield.</param>
public sealed record MediatrStream(int Count) : MediatR.IStreamRequest<int>;

/// <summary>Handles <see cref="MediatrStream"/>.</summary>
public sealed class MediatrStreamHandler : MediatR.IStreamRequestHandler<MediatrStream, int>
{
    /// <inheritdoc />
    public async IAsyncEnumerable<int> Handle(
        MediatrStream request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        for (int i = 0; i < request.Count; i++)
        {
            yield return i;
        }
    }
}
