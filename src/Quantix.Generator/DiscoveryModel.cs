// Equatable data the discovery stage produces (design section 6, stage 1; plan L2-A8, L2-B1).
// These types carry only strings, primitives and enums, so the incremental cache compares them
// by value and never holds a reference to a Roslyn symbol or compilation.

namespace Quantix.Generator;

/// <summary>
/// The five kinds of message Quantix dispatches.
/// </summary>
internal enum MessageKind
{
    /// <summary>An <c>ICommand</c> — performs an action and returns no result.</summary>
    VoidCommand,

    /// <summary>An <c>ICommand&lt;TResult&gt;</c> — performs an action and returns a result.</summary>
    Command,

    /// <summary>An <c>IQuery&lt;TResult&gt;</c> — returns data.</summary>
    Query,

    /// <summary>An <c>INotification</c> — broadcast to zero or more handlers.</summary>
    Notification,

    /// <summary>An <c>IStreamRequest&lt;TResult&gt;</c> — produces an asynchronous stream.</summary>
    StreamRequest,
}

/// <summary>
/// A handler discovered in a compilation: the correlation of a handler type with the message
/// it handles. Produced by the discovery stage and consumed by model building.
/// </summary>
/// <param name="Kind">The kind of message handled.</param>
/// <param name="HandlerType">The fully-qualified name of the handler type.</param>
/// <param name="MessageType">The fully-qualified name of the message type handled.</param>
/// <param name="ResultType">
/// The fully-qualified name of the result type, or null for a void command or a notification.
/// </param>
/// <param name="IsAbstract">Whether the handler type is abstract — reported by diagnostic QTX0003.</param>
/// <param name="Location">The handler type's source location, used when reporting diagnostics.</param>
/// <param name="Order">The <c>[NotificationOrder]</c> value; 0 when absent. Orders notification handlers.</param>
/// <param name="HasSignatureMismatch">
/// Whether the type declares a handler interface but does not implement its <c>Handle</c>
/// method — reported by diagnostic QTX0008.
/// </param>
/// <param name="IsOpenGeneric">
/// Whether the handler is a simple-shape open-generic handler for a generic message. Such a
/// handler is not dispatched directly; model building closes it over every concrete
/// instantiation of its message that occurs in the compilation.
/// </param>
/// <param name="OpenHandlerGenericName">
/// For an open-generic handler, its fully-qualified name without type parameters, used to emit
/// the closed handler type; null for a closed handler.
/// </param>
/// <param name="OpenMessageGenericName">
/// For an open-generic handler, the fully-qualified, parameterless name of the generic message
/// it handles, used to match the handler to concrete instantiations; null for a closed handler.
/// </param>
internal sealed record DiscoveredHandler(
    MessageKind Kind,
    string HandlerType,
    string MessageType,
    string? ResultType,
    bool IsAbstract,
    LocationInfo? Location,
    int Order,
    bool HasSignatureMismatch,
    bool IsOpenGeneric,
    string? OpenHandlerGenericName,
    string? OpenMessageGenericName);

/// <summary>
/// The three kinds of pipeline behavior, one per behavior interface.
/// </summary>
internal enum BehaviorKind
{
    /// <summary>An <c>IPipelineBehavior&lt;TRequest, TResult&gt;</c> — wraps a command or query.</summary>
    Request,

    /// <summary>An <c>ICommandPipelineBehavior&lt;TCommand&gt;</c> — wraps a void command.</summary>
    Command,

    /// <summary>An <c>IStreamPipelineBehavior&lt;TRequest, TResult&gt;</c> — wraps a stream request.</summary>
    Stream,
}

/// <summary>
/// The generic type-parameter constraints of a simple-shape open-generic behavior, captured so
/// model building can compute the exact set of messages the behavior wraps without re-resolving
/// symbols. Every constraint here applies to the behavior's <em>first</em> type parameter — the
/// message type — except <see cref="HasConstraintsBeyondFirstParameter"/>.
/// </summary>
/// <param name="RequiresReferenceType">The first type parameter has a <c>class</c> constraint.</param>
/// <param name="RequiresValueType">The first type parameter has a <c>struct</c> constraint.</param>
/// <param name="RequiresDefaultConstructor">The first type parameter has a <c>new()</c> constraint.</param>
/// <param name="RequiresUnmanagedType">The first type parameter has an <c>unmanaged</c> constraint.</param>
/// <param name="TypeConstraints">
/// The first type parameter's type constraints, each as a template in which the behavior's own
/// type parameters appear as positional placeholders (<c>{0}</c> for the message type, <c>{1}</c>
/// for the result type). Model building substitutes the message and tests set membership.
/// </param>
/// <param name="HasConstraintsBeyondFirstParameter">
/// Whether any type parameter other than the first carries a constraint. Quantix v1 evaluates
/// constraints on the message type parameter only, so such a behavior is not applied as an open
/// generic — closing it could otherwise violate the constraint and break the generated code.
/// </param>
internal sealed record BehaviorConstraints(
    bool RequiresReferenceType,
    bool RequiresValueType,
    bool RequiresDefaultConstructor,
    bool RequiresUnmanagedType,
    EquatableArray<string> TypeConstraints,
    bool HasConstraintsBeyondFirstParameter);

/// <summary>
/// A pipeline behavior discovered in a compilation. Produced by the discovery stage and
/// consumed by model building, which resolves the exact set of messages each behavior wraps.
/// </summary>
/// <param name="Kind">Which behavior interface the type implements.</param>
/// <param name="BehaviorType">The fully-qualified name of the behavior type.</param>
/// <param name="IsOpenGeneric">
/// Whether the behavior class is generic. An open-generic behavior wraps every message whose
/// type arguments satisfy its constraints; constraint scoping is resolved in model building.
/// </param>
/// <param name="Order">The <c>[PipelineOrder]</c> value; 0 when the attribute is absent.</param>
/// <param name="ClosedRequestType">
/// For a closed behavior, the fully-qualified request type it wraps; null for an open-generic behavior.
/// </param>
/// <param name="ClosedResultType">
/// For a closed request or stream behavior, the fully-qualified result type; null for a void-command
/// behavior or an open-generic behavior.
/// </param>
/// <param name="Location">The behavior type's source location, used when reporting diagnostics.</param>
/// <param name="IsSimpleShapeOpenGeneric">
/// True when the behavior is an open-generic behavior whose interface type arguments are exactly
/// its own type parameters, in order — so it can be closed positionally over a message. Only
/// these open-generic behaviors are applied; <see cref="Constraints"/> then scopes them.
/// </param>
/// <param name="OpenGenericName">
/// For an open-generic behavior, its fully-qualified name without type parameters, used to emit
/// the closed form; null for a closed behavior.
/// </param>
/// <param name="Constraints">
/// The first-type-parameter constraints of a simple-shape open-generic behavior; null for a
/// closed behavior or an open generic that is not simple-shape.
/// </param>
internal sealed record DiscoveredBehavior(
    BehaviorKind Kind,
    string BehaviorType,
    bool IsOpenGeneric,
    int Order,
    string? ClosedRequestType,
    string? ClosedResultType,
    LocationInfo? Location,
    bool IsSimpleShapeOpenGeneric,
    string? OpenGenericName,
    BehaviorConstraints? Constraints);

/// <summary>
/// A concrete message discovered in a compilation — either a non-generic message type
/// declaration, or a closed instantiation of a generic message (for example
/// <c>GetById&lt;Customer&gt;</c>) found at an object-creation site. Used to detect a message
/// that has no handler, to close generic-message handlers, and to evaluate whether a
/// constrained open-generic behavior wraps the message.
/// </summary>
/// <param name="Kind">The kind of message.</param>
/// <param name="MessageType">The fully-qualified name of the message type.</param>
/// <param name="ResultType">
/// The fully-qualified result type, or null for a void command or a notification.
/// </param>
/// <param name="Location">The message type's source location, used when reporting diagnostics.</param>
/// <param name="IsAmbiguous">Whether the type implements more than one message interface — reported by QTX0006.</param>
/// <param name="SatisfiedTypes">
/// Every type the message <em>is</em> — itself, every base type and every interface, each
/// fully-qualified. A type constraint <c>where T : X</c> is satisfied exactly when the
/// substituted <c>X</c> appears in this set.
/// </param>
/// <param name="IsReferenceType">
/// Whether the message is a reference type — satisfies a <c>class</c> constraint; a value-type
/// message satisfies a <c>struct</c> constraint instead.
/// </param>
/// <param name="HasDefaultConstructor">
/// Whether the message has a public parameterless constructor — satisfies a <c>new()</c> constraint.
/// </param>
/// <param name="IsUnmanagedType">Whether the message is an unmanaged type — satisfies an <c>unmanaged</c> constraint.</param>
/// <param name="OpenGenericName">
/// For a closed instantiation of a generic message, the parameterless fully-qualified name of
/// the generic message (used to match an open-generic handler); null for a non-generic message.
/// </param>
/// <param name="TypeArguments">
/// For a closed instantiation of a generic message, the closed type arguments — used to close
/// the open-generic handler over the same arguments; empty for a non-generic message.
/// </param>
internal sealed record DiscoveredMessage(
    MessageKind Kind,
    string MessageType,
    string? ResultType,
    LocationInfo? Location,
    bool IsAmbiguous,
    EquatableArray<string> SatisfiedTypes,
    bool IsReferenceType,
    bool HasDefaultConstructor,
    bool IsUnmanagedType,
    string? OpenGenericName,
    EquatableArray<string> TypeArguments);

/// <summary>
/// The result of the discovery transform for one candidate type: a handler, a pipeline
/// behavior, or a message. At most one property is non-null.
/// </summary>
/// <param name="Handler">The discovered handler, or null when the type is not a handler.</param>
/// <param name="Behavior">The discovered behavior, or null when the type is not a behavior.</param>
/// <param name="Message">The discovered message, or null when the type is not a message.</param>
internal sealed record DiscoveryResult(
    DiscoveredHandler? Handler,
    DiscoveredBehavior? Behavior,
    DiscoveredMessage? Message);
