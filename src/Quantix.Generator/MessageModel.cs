// The built model the generator emits from (design section 6, stage 3; plan L2-B).

namespace Quantix.Generator;

/// <summary>
/// A fully-resolved message: the message type, its handler (or, for a notification, its handler
/// set) and the ordered behavior chain that wraps it. Built by the model stage from the raw
/// discovery results and consumed by emission.
/// </summary>
/// <param name="Kind">The kind of message.</param>
/// <param name="MessageType">The fully-qualified name of the message type.</param>
/// <param name="ResultType">
/// The fully-qualified result type, or null for a void command or a notification.
/// </param>
/// <param name="Handlers">
/// The fully-qualified handler type names. A command, query or stream request has exactly one;
/// a notification has zero or more, in dispatch order.
/// </param>
/// <param name="Behaviors">
/// The fully-qualified behavior type names that wrap this message, ordered outermost first.
/// </param>
internal sealed record MessageModel(
    MessageKind Kind,
    string MessageType,
    string? ResultType,
    EquatableArray<string> Handlers,
    EquatableArray<string> Behaviors);

/// <summary>
/// The complete Quantix model for one compilation: every message the generator will emit a
/// dispatch path for, and every diagnostic the validation stage produced.
/// </summary>
/// <param name="Messages">Every resolved message in the compilation.</param>
/// <param name="Diagnostics">The diagnostics to report for this compilation.</param>
internal sealed record QuantixModel(
    EquatableArray<MessageModel> Messages,
    EquatableArray<DiagnosticInfo> Diagnostics);
