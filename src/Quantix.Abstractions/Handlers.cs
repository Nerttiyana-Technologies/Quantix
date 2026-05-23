// Quantix handler interfaces — one per message kind (design section 4.2).

namespace Quantix;

/// <summary>
/// Handles a single command that returns no result.
/// </summary>
/// <typeparam name="TCommand">The command type handled by this handler.</typeparam>
/// <remarks>
/// Every <see cref="ICommand"/> must have exactly one handler. A missing handler is reported
/// by diagnostic <c>QTX0001</c>; more than one is reported by <c>QTX0002</c>.
/// </remarks>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>Handles the command.</summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">A token that signals a request to cancel the operation.</param>
    /// <returns>A <see cref="System.Threading.Tasks.ValueTask"/> that completes when the command has been handled.</returns>
    ValueTask Handle(TCommand command, CancellationToken ct);
}

/// <summary>
/// Handles a single command that returns a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TCommand">The command type handled by this handler.</typeparam>
/// <typeparam name="TResult">The result type the command produces.</typeparam>
/// <remarks>
/// Every <see cref="ICommand{TResult}"/> must have exactly one handler. A missing handler is
/// reported by diagnostic <c>QTX0001</c>; more than one is reported by <c>QTX0002</c>.
/// </remarks>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>Handles the command and produces its result.</summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">A token that signals a request to cancel the operation.</param>
    /// <returns>A <see cref="System.Threading.Tasks.ValueTask{TResult}"/> yielding the command's result.</returns>
    ValueTask<TResult> Handle(TCommand command, CancellationToken ct);
}

/// <summary>
/// Handles a single query that returns a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TQuery">The query type handled by this handler.</typeparam>
/// <typeparam name="TResult">The result type the query produces.</typeparam>
/// <remarks>
/// Every <see cref="IQuery{TResult}"/> must have exactly one handler. A missing handler is
/// reported by diagnostic <c>QTX0001</c>; more than one is reported by <c>QTX0002</c>.
/// </remarks>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>Handles the query and produces its result.</summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="ct">A token that signals a request to cancel the operation.</param>
    /// <returns>A <see cref="System.Threading.Tasks.ValueTask{TResult}"/> yielding the query's result.</returns>
    ValueTask<TResult> Handle(TQuery query, CancellationToken ct);
}

/// <summary>
/// Handles a notification. A notification may have any number of handlers, including none.
/// </summary>
/// <typeparam name="TNotification">The notification type handled by this handler.</typeparam>
/// <remarks>
/// When a notification is published, its handlers run sequentially. Use
/// <see cref="NotificationOrderAttribute"/> to control the order.
/// </remarks>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>Handles the notification.</summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="ct">A token that signals a request to cancel the operation.</param>
    /// <returns>A <see cref="System.Threading.Tasks.ValueTask"/> that completes when the notification has been handled.</returns>
    ValueTask Handle(TNotification notification, CancellationToken ct);
}

/// <summary>
/// Handles a stream request, producing an asynchronous stream of results.
/// </summary>
/// <typeparam name="TRequest">The stream request type handled by this handler.</typeparam>
/// <typeparam name="TResult">The type of each item produced by the stream.</typeparam>
/// <remarks>
/// Every <see cref="IStreamRequest{TResult}"/> must have exactly one handler. A missing
/// handler is reported by diagnostic <c>QTX0004</c>.
/// </remarks>
public interface IStreamRequestHandler<in TRequest, TResult>
    where TRequest : IStreamRequest<TResult>
{
    /// <summary>Handles the stream request and produces its results.</summary>
    /// <param name="request">The stream request to handle.</param>
    /// <param name="ct">A token that signals a request to cancel the enumeration.</param>
    /// <returns>An <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/> of results.</returns>
    IAsyncEnumerable<TResult> Handle(TRequest request, CancellationToken ct);
}
