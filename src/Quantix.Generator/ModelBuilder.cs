// Builds the per-compilation model from the raw discovery results (design section 6, stage 3; plan L2-B).

using System.Collections.Immutable;

namespace Quantix.Generator;

/// <summary>
/// Correlates the flat discovery results into a <see cref="QuantixModel"/>: groups handlers by
/// the message they handle, attaches the closed pipeline behaviors that wrap each message,
/// validates the result, and produces one <see cref="MessageModel"/> per message.
/// </summary>
internal static class ModelBuilder
{
    /// <summary>
    /// Builds the model from the discovery results. The output is ordered deterministically by
    /// fully-qualified type name, so the generated source is stable across builds.
    /// </summary>
    /// <param name="declarations">The handlers, behaviors and messages discovered from type declarations.</param>
    /// <param name="instantiations">
    /// The concrete instantiations of generic messages discovered at object-creation sites.
    /// </param>
    /// <param name="reportDiscovery">
    /// Whether to emit the opt-in QTX0010 discovery info for every handler and behavior.
    /// </param>
    /// <returns>The resolved model.</returns>
    public static QuantixModel Build(
        ImmutableArray<DiscoveryResult> declarations,
        ImmutableArray<DiscoveredMessage> instantiations,
        bool reportDiscovery)
    {
        ImmutableArray<DiagnosticInfo>.Builder diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        // Open-generic handlers are closed over every concrete instantiation of their message;
        // the resulting closed handlers and instantiated messages join the discovery results, so
        // the rest of model building treats a generic message exactly like an ordinary one.
        List<DiscoveredHandler> openGenericHandlers = CollectOpenGenericHandlers(declarations);
        ImmutableArray<DiscoveryResult> results =
            AugmentWithGenericMessages(declarations, instantiations, openGenericHandlers, diagnostics);

        Dictionary<string, List<DiscoveredHandler>> handlersByMessage = GroupHandlersByMessage(results);
        Dictionary<string, DiscoveredMessage> messagesByType = BuildMessageIndex(results);
        List<DiscoveredBehavior> closedBehaviors = CollectClosedBehaviors(results);
        List<DiscoveredBehavior> openGenericBehaviors = CollectOpenGenericBehaviors(results);

        ImmutableArray<MessageModel>.Builder messages =
            ImmutableArray.CreateBuilder<MessageModel>(handlersByMessage.Count);

        foreach (string messageType in handlersByMessage.Keys.OrderBy(static key => key, StringComparer.Ordinal))
        {
            List<DiscoveredHandler> handlers = handlersByMessage[messageType];
            handlers.Sort(static (left, right) =>
            {
                int byOrder = left.Order.CompareTo(right.Order);
                return byOrder != 0 ? byOrder : string.CompareOrdinal(left.HandlerType, right.HandlerType);
            });

            DiscoveredHandler first = handlers[0];
            Validate(messageType, first.Kind, handlers, diagnostics);

            ImmutableArray<string>.Builder handlerTypes = ImmutableArray.CreateBuilder<string>(handlers.Count);
            foreach (DiscoveredHandler handler in handlers)
            {
                handlerTypes.Add(handler.HandlerType);
            }

            messagesByType.TryGetValue(messageType, out DiscoveredMessage? message);

            messages.Add(new MessageModel(
                first.Kind,
                messageType,
                first.ResultType,
                new EquatableArray<string>(handlerTypes.ToImmutable()),
                ResolveBehaviorChain(
                    messageType,
                    first.ResultType,
                    first.Kind,
                    message,
                    closedBehaviors,
                    openGenericBehaviors,
                    diagnostics)));
        }

        ValidateMessages(results, handlersByMessage, diagnostics);
        ValidateBehaviors(results, diagnostics);

        if (reportDiscovery)
        {
            ReportDiscovery(declarations, diagnostics);
        }

        return new QuantixModel(
            new EquatableArray<MessageModel>(messages.ToImmutable()),
            new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutable()));
    }

    /// <summary>Collects the simple-shape open-generic handlers discovered in the compilation.</summary>
    private static List<DiscoveredHandler> CollectOpenGenericHandlers(ImmutableArray<DiscoveryResult> declarations)
    {
        var openGeneric = new List<DiscoveredHandler>();

        foreach (DiscoveryResult result in declarations)
        {
            if (result.Handler is { IsOpenGeneric: true } handler)
            {
                openGeneric.Add(handler);
            }
        }

        return openGeneric;
    }

    /// <summary>
    /// Produces the augmented discovery results that model building runs on: every declaration
    /// except the open-generic handlers, plus one closed message and one closed handler for each
    /// concrete instantiation of a generic message. An open-generic handler that matches no
    /// instantiation is reported by QTX0011.
    /// </summary>
    private static ImmutableArray<DiscoveryResult> AugmentWithGenericMessages(
        ImmutableArray<DiscoveryResult> declarations,
        ImmutableArray<DiscoveredMessage> instantiations,
        List<DiscoveredHandler> openGenericHandlers,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        ImmutableArray<DiscoveryResult>.Builder builder = ImmutableArray.CreateBuilder<DiscoveryResult>();

        // An open-generic handler is never dispatched directly — only its closed forms are.
        foreach (DiscoveryResult result in declarations)
        {
            if (result.Handler is not { IsOpenGeneric: true })
            {
                builder.Add(result);
            }
        }

        // The same generic message may be constructed at many sites; keep one entry per type.
        var distinctInstantiations = new Dictionary<string, DiscoveredMessage>(StringComparer.Ordinal);
        foreach (DiscoveredMessage instantiation in instantiations)
        {
            distinctInstantiations[instantiation.MessageType] = instantiation;
        }

        var matchedOpenHandlers = new HashSet<string>(StringComparer.Ordinal);

        foreach (DiscoveredMessage instantiation in distinctInstantiations.Values)
        {
            builder.Add(new DiscoveryResult(null, null, instantiation));

            foreach (DiscoveredHandler openHandler in openGenericHandlers)
            {
                if (string.Equals(openHandler.OpenMessageGenericName, instantiation.OpenGenericName, StringComparison.Ordinal))
                {
                    matchedOpenHandlers.Add(openHandler.HandlerType);
                    builder.Add(new DiscoveryResult(CloseHandler(openHandler, instantiation), null, null));
                }
            }
        }

        // QTX0011 — an open-generic handler with no concrete instantiation of its message.
        openGenericHandlers.Sort(static (left, right) => string.CompareOrdinal(left.HandlerType, right.HandlerType));
        foreach (DiscoveredHandler openHandler in openGenericHandlers)
        {
            if (!matchedOpenHandlers.Contains(openHandler.HandlerType))
            {
                diagnostics.Add(new DiagnosticInfo(
                    QuantixDiagnostics.UnusedOpenGenericHandler,
                    openHandler.Location,
                    new EquatableArray<string>(ImmutableArray.Create(openHandler.HandlerType))));
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Closes an open-generic handler over a concrete message instantiation, producing a fully
    /// closed handler the rest of the pipeline can treat as an ordinary discovered handler.
    /// </summary>
    private static DiscoveredHandler CloseHandler(DiscoveredHandler openHandler, DiscoveredMessage instantiation)
        => new DiscoveredHandler(
            openHandler.Kind,
            $"{openHandler.OpenHandlerGenericName}<{JoinTypeArguments(instantiation.TypeArguments)}>",
            instantiation.MessageType,
            instantiation.ResultType,
            openHandler.IsAbstract,
            openHandler.Location,
            openHandler.Order,
            openHandler.HasSignatureMismatch,
            IsOpenGeneric: false,
            OpenHandlerGenericName: null,
            OpenMessageGenericName: null);

    /// <summary>Joins the closed type arguments of a generic message into a <c>T1, T2</c> list.</summary>
    private static string JoinTypeArguments(EquatableArray<string> typeArguments)
    {
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < typeArguments.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(typeArguments[i]);
        }

        return builder.ToString();
    }

    /// <summary>Validates the handlers of one message, appending any diagnostics found.</summary>
    private static void Validate(
        string messageType,
        MessageKind kind,
        List<DiscoveredHandler> handlers,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        foreach (DiscoveredHandler handler in handlers)
        {
            // QTX0003 — an abstract handler cannot be constructed by the container.
            if (handler.IsAbstract)
            {
                diagnostics.Add(new DiagnosticInfo(
                    QuantixDiagnostics.HandlerNotConstructable,
                    handler.Location,
                    new EquatableArray<string>(ImmutableArray.Create(handler.HandlerType))));
            }

            // QTX0008 — the type declares a handler interface but does not implement Handle.
            if (handler.HasSignatureMismatch)
            {
                diagnostics.Add(new DiagnosticInfo(
                    QuantixDiagnostics.HandlerSignatureMismatch,
                    handler.Location,
                    new EquatableArray<string>(ImmutableArray.Create(handler.HandlerType))));
            }
        }

        // QTX0002 — a command, query or stream request must have exactly one handler.
        if (kind != MessageKind.Notification && handlers.Count > 1)
        {
            diagnostics.Add(new DiagnosticInfo(
                QuantixDiagnostics.MultipleHandlers,
                handlers[0].Location,
                new EquatableArray<string>(ImmutableArray.Create(messageType))));
        }
    }

    /// <summary>
    /// Checks every discovered message for a handler, reporting QTX0001 / QTX0004 / QTX0005
    /// when a command, query, stream request or notification has none.
    /// </summary>
    private static void ValidateMessages(
        ImmutableArray<DiscoveryResult> results,
        Dictionary<string, List<DiscoveredHandler>> handlersByMessage,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var messages = new List<DiscoveredMessage>();
        foreach (DiscoveryResult result in results)
        {
            if (result.Message is { } message)
            {
                messages.Add(message);
            }
        }

        messages.Sort(static (left, right) => string.CompareOrdinal(left.MessageType, right.MessageType));

        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (DiscoveredMessage message in messages)
        {
            if (!reported.Add(message.MessageType))
            {
                continue;
            }

            // QTX0006 — a type that implements more than one message interface.
            if (message.IsAmbiguous)
            {
                diagnostics.Add(new DiagnosticInfo(
                    QuantixDiagnostics.AmbiguousResultType,
                    message.Location,
                    new EquatableArray<string>(ImmutableArray.Create(message.MessageType))));
                continue;
            }

            if (handlersByMessage.ContainsKey(message.MessageType))
            {
                continue;
            }

            // QTX0001 / QTX0004 / QTX0005 — the message has no handler.
            var descriptor = message.Kind switch
            {
                MessageKind.StreamRequest => QuantixDiagnostics.NoStreamHandler,
                MessageKind.Notification => QuantixDiagnostics.NotificationHasNoHandlers,
                _ => QuantixDiagnostics.NoHandler,
            };

            diagnostics.Add(new DiagnosticInfo(
                descriptor,
                message.Location,
                new EquatableArray<string>(ImmutableArray.Create(message.MessageType))));
        }
    }

    /// <summary>
    /// Reports QTX0009 for every closed behavior whose request type is not a Quantix message.
    /// </summary>
    private static void ValidateBehaviors(
        ImmutableArray<DiscoveryResult> results,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var messageTypes = new HashSet<string>(StringComparer.Ordinal);
        var behaviors = new List<DiscoveredBehavior>();

        foreach (DiscoveryResult result in results)
        {
            if (result.Message is { } message)
            {
                messageTypes.Add(message.MessageType);
            }
            else if (result.Behavior is { IsOpenGeneric: false } behavior && behavior.ClosedRequestType is not null)
            {
                behaviors.Add(behavior);
            }
        }

        behaviors.Sort(static (left, right) => string.CompareOrdinal(left.BehaviorType, right.BehaviorType));

        foreach (DiscoveredBehavior behavior in behaviors)
        {
            if (!messageTypes.Contains(behavior.ClosedRequestType!))
            {
                diagnostics.Add(new DiagnosticInfo(
                    QuantixDiagnostics.BehaviorOverNonMessage,
                    behavior.Location,
                    new EquatableArray<string>(
                        ImmutableArray.Create(behavior.BehaviorType, behavior.ClosedRequestType!))));
            }
        }
    }

    /// <summary>
    /// Emits the opt-in QTX0010 discovery info for every discovered handler and behavior, ordered
    /// by type name so the diagnostic stream is stable across builds.
    /// </summary>
    private static void ReportDiscovery(
        ImmutableArray<DiscoveryResult> results,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var discovered = new List<(string TypeName, LocationInfo? Location)>();

        foreach (DiscoveryResult result in results)
        {
            if (result.Handler is { } handler)
            {
                discovered.Add((handler.HandlerType, handler.Location));
            }
            else if (result.Behavior is { } behavior)
            {
                discovered.Add((behavior.BehaviorType, behavior.Location));
            }
        }

        discovered.Sort(static (left, right) => string.CompareOrdinal(left.TypeName, right.TypeName));

        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach ((string typeName, LocationInfo? location) in discovered)
        {
            if (reported.Add(typeName))
            {
                diagnostics.Add(new DiagnosticInfo(
                    QuantixDiagnostics.DiscoveryInfo,
                    location,
                    new EquatableArray<string>(ImmutableArray.Create(typeName))));
            }
        }
    }

    /// <summary>Groups every discovered handler by the fully-qualified name of its message type.</summary>
    private static Dictionary<string, List<DiscoveredHandler>> GroupHandlersByMessage(
        ImmutableArray<DiscoveryResult> results)
    {
        var grouped = new Dictionary<string, List<DiscoveredHandler>>(StringComparer.Ordinal);

        foreach (DiscoveryResult result in results)
        {
            if (result.Handler is { } handler)
            {
                if (!grouped.TryGetValue(handler.MessageType, out List<DiscoveredHandler>? handlers))
                {
                    handlers = new List<DiscoveredHandler>();
                    grouped.Add(handler.MessageType, handlers);
                }

                handlers.Add(handler);
            }
        }

        return grouped;
    }

    /// <summary>Collects the closed (non-open-generic) behaviors discovered in the compilation.</summary>
    private static List<DiscoveredBehavior> CollectClosedBehaviors(ImmutableArray<DiscoveryResult> results)
    {
        var closed = new List<DiscoveredBehavior>();

        foreach (DiscoveryResult result in results)
        {
            if (result.Behavior is { IsOpenGeneric: false } behavior && behavior.ClosedRequestType is not null)
            {
                closed.Add(behavior);
            }
        }

        return closed;
    }

    /// <summary>
    /// Collects the simple-shape open-generic behaviors discovered in the compilation — those
    /// that can be closed positionally over a message. Each behavior's constraints scope it to
    /// the exact set of messages it wraps; an unconstrained one wraps every applicable message.
    /// </summary>
    private static List<DiscoveredBehavior> CollectOpenGenericBehaviors(ImmutableArray<DiscoveryResult> results)
    {
        var openGeneric = new List<DiscoveredBehavior>();

        foreach (DiscoveryResult result in results)
        {
            if (result.Behavior is { IsSimpleShapeOpenGeneric: true } behavior)
            {
                openGeneric.Add(behavior);
            }
        }

        return openGeneric;
    }

    /// <summary>
    /// Indexes every discovered message by its fully-qualified type name. The index supplies the
    /// type information — satisfied types, reference/value kind, constructor — that constraint
    /// evaluation needs to scope an open-generic behavior to a message.
    /// </summary>
    private static Dictionary<string, DiscoveredMessage> BuildMessageIndex(ImmutableArray<DiscoveryResult> results)
    {
        var index = new Dictionary<string, DiscoveredMessage>(StringComparer.Ordinal);

        foreach (DiscoveryResult result in results)
        {
            if (result.Message is { } message)
            {
                index[message.MessageType] = message;
            }
        }

        return index;
    }

    /// <summary>
    /// Builds the ordered behavior chain for a message: the closed behaviors that target it, plus
    /// every open-generic behavior whose constraints the message satisfies, closed over the
    /// message. Behaviors run lowest <c>[PipelineOrder]</c> first; ties break by fully-qualified
    /// type name.
    /// </summary>
    private static EquatableArray<string> ResolveBehaviorChain(
        string messageType,
        string? resultType,
        MessageKind kind,
        DiscoveredMessage? message,
        List<DiscoveredBehavior> closedBehaviors,
        List<DiscoveredBehavior> openGenericBehaviors,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        var matched = new List<(DiscoveredBehavior Behavior, string Name)>();

        foreach (DiscoveredBehavior behavior in closedBehaviors)
        {
            if (behavior.ClosedRequestType == messageType && AppliesToKind(behavior.Kind, kind))
            {
                matched.Add((behavior, behavior.BehaviorType));
            }
        }

        foreach (DiscoveredBehavior behavior in openGenericBehaviors)
        {
            if (AppliesToKind(behavior.Kind, kind) && BehaviorApplies(behavior, message, resultType))
            {
                matched.Add((behavior, CloseOpenGeneric(behavior, messageType, resultType)));
            }
        }

        if (matched.Count == 0)
        {
            return EquatableArray<string>.Empty;
        }

        matched.Sort(static (left, right) =>
        {
            int byOrder = left.Behavior.Order.CompareTo(right.Behavior.Order);
            return byOrder != 0 ? byOrder : string.CompareOrdinal(left.Name, right.Name);
        });

        ImmutableArray<string>.Builder chain = ImmutableArray.CreateBuilder<string>(matched.Count);
        for (int i = 0; i < matched.Count; i++)
        {
            chain.Add(matched[i].Name);

            // QTX0007 — adjacent behaviors with an equal [PipelineOrder] were ordered by name.
            if (i > 0 && matched[i - 1].Behavior.Order == matched[i].Behavior.Order)
            {
                diagnostics.Add(new DiagnosticInfo(
                    QuantixDiagnostics.DuplicatePipelineOrder,
                    matched[i].Behavior.Location,
                    new EquatableArray<string>(ImmutableArray.Create(
                        matched[i - 1].Behavior.BehaviorType,
                        matched[i].Behavior.BehaviorType))));
            }
        }

        return new EquatableArray<string>(chain.ToImmutable());
    }

    /// <summary>Determines whether a behavior of the given kind can wrap a message of the given kind.</summary>
    private static bool AppliesToKind(BehaviorKind behaviorKind, MessageKind messageKind)
        => behaviorKind switch
        {
            BehaviorKind.Request => messageKind is MessageKind.Command or MessageKind.Query,
            BehaviorKind.Command => messageKind == MessageKind.VoidCommand,
            BehaviorKind.Stream => messageKind == MessageKind.StreamRequest,
            _ => false,
        };

    /// <summary>
    /// Determines whether an open-generic behavior's generic constraints are satisfied by the
    /// message — that is, whether closing the behavior over the message would compile. An
    /// unconstrained behavior always applies.
    /// </summary>
    private static bool BehaviorApplies(DiscoveredBehavior behavior, DiscoveredMessage? message, string? resultType)
    {
        if (behavior.Constraints is not { } constraints)
        {
            return true;
        }

        // Quantix v1 evaluates constraints on the message type parameter only; a behavior that
        // also constrains its result type parameter is not applied as an open generic.
        if (constraints.HasConstraintsBeyondFirstParameter)
        {
            return false;
        }

        if (message is null)
        {
            // The message was not declared in this compilation, so its type information is
            // unavailable; only an unconstrained behavior can be proven to apply.
            return constraints is
                {
                    RequiresReferenceType: false,
                    RequiresValueType: false,
                    RequiresDefaultConstructor: false,
                    RequiresUnmanagedType: false,
                }
                && constraints.TypeConstraints.Count == 0;
        }

        if (constraints.RequiresReferenceType && !message.IsReferenceType)
        {
            return false;
        }

        if (constraints.RequiresValueType && message.IsReferenceType)
        {
            return false;
        }

        if (constraints.RequiresDefaultConstructor && !message.HasDefaultConstructor)
        {
            return false;
        }

        if (constraints.RequiresUnmanagedType && !message.IsUnmanagedType)
        {
            return false;
        }

        for (int i = 0; i < constraints.TypeConstraints.Count; i++)
        {
            string required = SubstitutePlaceholders(constraints.TypeConstraints[i], message.MessageType, resultType);
            if (!ContainsOrdinal(message.SatisfiedTypes, required))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Substitutes the message type into <c>{0}</c> and the result type into <c>{1}</c> of a
    /// behavior constraint template, yielding the concrete type the message must satisfy.
    /// </summary>
    private static string SubstitutePlaceholders(string template, string messageType, string? resultType)
    {
        string substituted = template.Replace("{0}", messageType);
        return resultType is null ? substituted : substituted.Replace("{1}", resultType);
    }

    /// <summary>Determines whether an equatable array contains the target string by ordinal comparison.</summary>
    private static bool ContainsOrdinal(EquatableArray<string> values, string target)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (string.Equals(values[i], target, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Closes an open-generic behavior over a message: a void-command behavior takes the message
    /// type alone; a request or stream behavior takes the message type and its result type.
    /// </summary>
    private static string CloseOpenGeneric(DiscoveredBehavior behavior, string messageType, string? resultType)
        => behavior.Kind == BehaviorKind.Command
            ? $"{behavior.OpenGenericName}<{messageType}>"
            : $"{behavior.OpenGenericName}<{messageType}, {resultType}>";
}
