// Quantix attributes — all optional; discovery works with none of them (design section 4.5).

namespace Quantix;

/// <summary>
/// Sets the position of a pipeline behavior within the behavior chain.
/// </summary>
/// <remarks>
/// Behaviors run from the lowest <see cref="Order"/> (outermost) to the highest (innermost,
/// closest to the handler). The default order is <c>0</c>. Ties are broken by fully-qualified
/// type name so the generated chain is stable across builds.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PipelineOrderAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="PipelineOrderAttribute"/> class.</summary>
    /// <param name="order">The behavior's position in the chain; lower values run first.</param>
    public PipelineOrderAttribute(int order)
    {
        Order = order;
    }

    /// <summary>Gets the behavior's position in the chain; lower values run first.</summary>
    public int Order { get; }
}

/// <summary>
/// Sets the sequential execution order of a notification handler.
/// </summary>
/// <remarks>
/// When a notification is published its handlers run one at a time in ascending
/// <see cref="Order"/>. The default order is <c>0</c>; ties are broken by fully-qualified
/// type name.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NotificationOrderAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="NotificationOrderAttribute"/> class.</summary>
    /// <param name="order">The handler's execution order; lower values run first.</param>
    public NotificationOrderAttribute(int order)
    {
        Order = order;
    }

    /// <summary>Gets the handler's execution order; lower values run first.</summary>
    public int Order { get; }
}

/// <summary>
/// Excludes a handler or behavior from Quantix discovery.
/// </summary>
/// <remarks>
/// Useful for a test double or an alternative implementation that should not be wired into
/// the generated dispatcher.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class QuantixIgnoreAttribute : Attribute
{
}

/// <summary>
/// Explicitly opts a type in to Quantix discovery.
/// </summary>
/// <remarks>
/// Never required — discovery works from the handler interfaces alone. This attribute exists
/// for clarity, or to force inclusion of a generic edge-case type.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class QuantixHandlerAttribute : Attribute
{
}

/// <summary>
/// Marks an assembly as a participant in Quantix cross-assembly discovery.
/// </summary>
/// <remarks>
/// Apply at assembly level. It makes the assembly's intent to contribute handlers explicit;
/// the composition root's generator composes every such module automatically.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class QuantixModuleAttribute : Attribute
{
}

/// <summary>
/// Pulls Quantix handlers from an assembly that does not itself reference Quantix.
/// </summary>
/// <remarks>
/// Apply at assembly level; it may be applied more than once. The generator inspects the
/// assembly that declares <see cref="MarkerType"/> for handlers and behaviors.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class ScanQuantixHandlersFromAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="ScanQuantixHandlersFromAttribute"/> class.</summary>
    /// <param name="markerType">Any type declared in the assembly that should be scanned for handlers.</param>
    public ScanQuantixHandlersFromAttribute(Type markerType)
    {
        MarkerType = markerType;
    }

    /// <summary>Gets a type declared in the assembly that should be scanned for handlers.</summary>
    public Type MarkerType { get; }
}
