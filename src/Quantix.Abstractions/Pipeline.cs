// Quantix pipeline behaviors and the delegates that chain them (design section 4.4).

namespace Quantix;

/// <summary>
/// Represents the continuation of a request pipeline: invokes the next behavior, or the
/// handler itself once the end of the chain is reached.
/// </summary>
/// <typeparam name="TResult">The result type produced by the request.</typeparam>
/// <param name="ct">A token that signals a request to cancel the operation.</param>
/// <returns>A <see cref="System.Threading.Tasks.ValueTask{TResult}"/> yielding the request's result.</returns>
public delegate ValueTask<TResult> RequestHandlerDelegate<TResult>(CancellationToken ct);

/// <summary>
/// Represents the continuation of a void-command pipeline: invokes the next behavior, or the
/// handler itself once the end of the chain is reached.
/// </summary>
/// <param name="ct">A token that signals a request to cancel the operation.</param>
/// <returns>A <see cref="System.Threading.Tasks.ValueTask"/> that completes when the command has been handled.</returns>
public delegate ValueTask CommandHandlerDelegate(CancellationToken ct);

/// <summary>
/// Represents the continuation of a stream pipeline: invokes the next behavior, or the
/// handler itself once the end of the chain is reached.
/// </summary>
/// <typeparam name="TResult">The type of each item in the stream.</typeparam>
/// <param name="ct">A token that signals a request to cancel the enumeration.</param>
/// <returns>An <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/> of results.</returns>
public delegate IAsyncEnumerable<TResult> StreamHandlerDelegate<TResult>(CancellationToken ct);

/// <summary>
/// A pipeline behavior that wraps the handling of a command or query that returns a result —
/// the place for cross-cutting concerns such as logging, validation, caching or retries.
/// </summary>
/// <typeparam name="TRequest">The request type wrapped by this behavior.</typeparam>
/// <typeparam name="TResult">The result type the request produces.</typeparam>
/// <remarks>
/// <para>
/// A behavior may be closed (it names a concrete request type) or open-generic. An
/// open-generic behavior wraps every message whose type arguments satisfy the behavior
/// class's own generic constraints — this is constraint-aware scoping.
/// </para>
/// <para>
/// Apply <see cref="PipelineOrderAttribute"/> to control where the behavior sits in the
/// chain; behaviors run from the lowest order (outermost) to the highest (innermost, closest
/// to the handler).
/// </para>
/// </remarks>
public interface IPipelineBehavior<in TRequest, TResult>
{
    /// <summary>Handles the request, optionally invoking the rest of the pipeline.</summary>
    /// <param name="request">The request flowing through the pipeline.</param>
    /// <param name="next">The continuation that invokes the rest of the pipeline.</param>
    /// <param name="ct">A token that signals a request to cancel the operation.</param>
    /// <returns>A <see cref="System.Threading.Tasks.ValueTask{TResult}"/> yielding the result.</returns>
    ValueTask<TResult> Handle(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct);
}

/// <summary>
/// A pipeline behavior that wraps the handling of a void command.
/// </summary>
/// <typeparam name="TCommand">The command type wrapped by this behavior.</typeparam>
/// <remarks>
/// A separate interface from <see cref="IPipelineBehavior{TRequest, TResult}"/> so that no
/// synthetic unit type is ever required for behaviors over void commands.
/// </remarks>
public interface ICommandPipelineBehavior<in TCommand>
    where TCommand : ICommand
{
    /// <summary>Handles the command, optionally invoking the rest of the pipeline.</summary>
    /// <param name="command">The command flowing through the pipeline.</param>
    /// <param name="next">The continuation that invokes the rest of the pipeline.</param>
    /// <param name="ct">A token that signals a request to cancel the operation.</param>
    /// <returns>A <see cref="System.Threading.Tasks.ValueTask"/> that completes when the command has been handled.</returns>
    ValueTask Handle(TCommand command, CommandHandlerDelegate next, CancellationToken ct);
}

/// <summary>
/// A pipeline behavior that wraps the handling of a stream request.
/// </summary>
/// <typeparam name="TRequest">The stream request type wrapped by this behavior.</typeparam>
/// <typeparam name="TResult">The type of each item in the stream.</typeparam>
public interface IStreamPipelineBehavior<in TRequest, TResult>
    where TRequest : IStreamRequest<TResult>
{
    /// <summary>Handles the stream request, optionally invoking the rest of the pipeline.</summary>
    /// <param name="request">The stream request flowing through the pipeline.</param>
    /// <param name="next">The continuation that invokes the rest of the pipeline.</param>
    /// <param name="ct">A token that signals a request to cancel the enumeration.</param>
    /// <returns>An <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/> of results.</returns>
    IAsyncEnumerable<TResult> Handle(TRequest request, StreamHandlerDelegate<TResult> next, CancellationToken ct);
}
