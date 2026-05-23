// Quantix message markers — the data objects sent through the mediator (design section 4.1).

namespace Quantix;

/// <summary>
/// Marks a type as a command: a message that performs an action and returns no result.
/// </summary>
/// <remarks>
/// <para>
/// A command expresses intent to change state. It is dispatched through the mediator's
/// <c>Send</c> verb and is processed by exactly one <see cref="ICommandHandler{TCommand}"/>.
/// </para>
/// <para>
/// This interface is a pure marker and declares no members. It exists so the Quantix source
/// generator and the C# compiler can recognise the type's role at compile time; a command
/// with no handler is reported as a build error rather than a runtime failure.
/// </para>
/// </remarks>
public interface ICommand
{
}

/// <summary>
/// Marks a type as a command that performs an action and returns a result of type
/// <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type of result the command produces.</typeparam>
/// <remarks>
/// <para>
/// A command expresses intent to change state. It is dispatched through the mediator's
/// <c>Send</c> verb and is processed by exactly one
/// <see cref="ICommandHandler{TCommand, TResult}"/>.
/// </para>
/// <para>
/// <typeparamref name="TResult"/> is deliberately invariant — there is no <c>out</c>
/// variance. Invariance guarantees the declared result type is exactly the type the
/// generator dispatches, which is what keeps Quantix dispatch allocation-free.
/// </para>
/// </remarks>
public interface ICommand<TResult>
{
}

/// <summary>
/// Marks a type as a query: a message that returns data of type <typeparamref name="TResult"/>
/// and, by convention, causes no side effects.
/// </summary>
/// <typeparam name="TResult">The type of result the query produces.</typeparam>
/// <remarks>
/// <para>
/// A query reads state. It is dispatched through the mediator's <c>Send</c> verb and is
/// processed by exactly one <see cref="IQueryHandler{TQuery, TResult}"/>.
/// </para>
/// <para>
/// <see cref="IQuery{TResult}"/> and <see cref="ICommand{TResult}"/> are structurally
/// identical; they are kept distinct purely to express intent — commands change state,
/// queries read it. A type may implement at most one of them.
/// </para>
/// </remarks>
public interface IQuery<TResult>
{
}

/// <summary>
/// Marks a type as a notification: an event broadcast to zero or more handlers.
/// </summary>
/// <remarks>
/// <para>
/// A notification is dispatched through the mediator's <c>Publish</c> verb. Every
/// <see cref="INotificationHandler{TNotification}"/> registered for the type is invoked
/// sequentially, each awaited in turn before the next.
/// </para>
/// <para>
/// Unlike a command or query, a notification may have no handlers at all, in which case
/// publishing it is a no-op.
/// </para>
/// </remarks>
public interface INotification
{
}

/// <summary>
/// Marks a type as a stream request: a message that produces an asynchronous stream of
/// results of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type of each item produced by the stream.</typeparam>
/// <remarks>
/// A stream request is dispatched through the mediator's <c>Stream</c> verb and is processed
/// by exactly one <see cref="IStreamRequestHandler{TRequest, TResult}"/>, which returns an
/// <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/>.
/// </remarks>
public interface IStreamRequest<TResult>
{
}
