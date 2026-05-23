// The catalogue of QTX diagnostics the generator reports (design section 9; plan L2-C1).

using Microsoft.CodeAnalysis;

namespace Quantix.Generator;

/// <summary>
/// The <c>QTX</c> diagnostics Quantix reports at compile time. Reporting a problem here turns a
/// would-be runtime failure — a missing handler, a duplicate handler — into a build error.
/// </summary>
internal static class QuantixDiagnostics
{
    private const string Category = "Quantix";

    /// <summary>QTX0001 — no handler is registered for a command or query.</summary>
    public static readonly DiagnosticDescriptor NoHandler = new(
        id: "QTX0001",
        title: "No handler registered for message",
        messageFormat: "No Quantix handler is registered for '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>QTX0002 — more than one handler exists for a single-handler message.</summary>
    public static readonly DiagnosticDescriptor MultipleHandlers = new(
        id: "QTX0002",
        title: "Multiple handlers registered for message",
        messageFormat: "More than one Quantix handler is registered for '{0}'; commands, queries and stream requests must have exactly one",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>QTX0003 — a discovered handler is abstract or has no usable constructor.</summary>
    public static readonly DiagnosticDescriptor HandlerNotConstructable = new(
        id: "QTX0003",
        title: "Handler cannot be constructed",
        messageFormat: "Quantix handler '{0}' cannot be constructed because it is abstract",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>QTX0004 — a type is used as a stream request but has no stream handler.</summary>
    public static readonly DiagnosticDescriptor NoStreamHandler = new(
        id: "QTX0004",
        title: "No handler registered for stream request",
        messageFormat: "No Quantix stream handler is registered for '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>QTX0005 — a notification has no handlers, so publishing it is a no-op.</summary>
    public static readonly DiagnosticDescriptor NotificationHasNoHandlers = new(
        id: "QTX0005",
        title: "Notification has no handlers",
        messageFormat: "Notification '{0}' has no handlers; publishing it will be a no-op",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>QTX0006 — a message has an ambiguous result type.</summary>
    public static readonly DiagnosticDescriptor AmbiguousResultType = new(
        id: "QTX0006",
        title: "Ambiguous message result type",
        messageFormat: "Message '{0}' has an ambiguous result type; it must implement exactly one message interface, closed over a single result type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>QTX0007 — two behaviors share a [PipelineOrder] value.</summary>
    public static readonly DiagnosticDescriptor DuplicatePipelineOrder = new(
        id: "QTX0007",
        title: "Duplicate pipeline order",
        messageFormat: "Behaviors '{0}' and '{1}' share the same [PipelineOrder]; their order was resolved by type name",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>QTX0008 — a handler's Handle method does not match its handler interface.</summary>
    public static readonly DiagnosticDescriptor HandlerSignatureMismatch = new(
        id: "QTX0008",
        title: "Handler signature mismatch",
        messageFormat: "Quantix handler '{0}' has a 'Handle' method that does not match its handler interface",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>QTX0009 — a behavior closes over a type that is not a Quantix message.</summary>
    public static readonly DiagnosticDescriptor BehaviorOverNonMessage = new(
        id: "QTX0009",
        title: "Behavior targets a non-message type",
        messageFormat: "Behavior '{0}' closes over '{1}', which is not a Quantix message",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// QTX0010 — opt-in: a handler or behavior was discovered and registered. The diagnostic is
    /// enabled by default, but the generator only emits it when the
    /// <c>QuantixReportDiscovery</c> MSBuild property is set, so it is silent unless requested.
    /// </summary>
    public static readonly DiagnosticDescriptor DiscoveryInfo = new(
        id: "QTX0010",
        title: "Quantix discovery",
        messageFormat: "Quantix discovered and registered '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>QTX0011 — an open-generic handler has no concrete message instantiation.</summary>
    public static readonly DiagnosticDescriptor UnusedOpenGenericHandler = new(
        id: "QTX0011",
        title: "Open-generic handler is never instantiated",
        messageFormat: "Open-generic handler '{0}' has no concrete message instantiation; no dispatch path was generated",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// QTX0012 — the navigation hint: identifies, at a <c>Send</c>, <c>Publish</c> or
    /// <c>Stream</c> call site, the handler that runs. It is informational and never affects a
    /// build; it ends the "blindfold" by naming the handler directly at the call.
    /// </summary>
    public static readonly DiagnosticDescriptor HandlerHint = new(
        id: "QTX0012",
        title: "Quantix handler",
        messageFormat: "Quantix: '{0}' is handled by {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}
