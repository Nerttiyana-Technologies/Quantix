// Quantix.QuantixOptions — registration-time configuration (design section 8.5).

using Microsoft.Extensions.DependencyInjection;

namespace Quantix;

/// <summary>
/// Configures Quantix at registration time. An instance is passed to the generated
/// <c>AddQuantix</c> extension method.
/// </summary>
/// <remarks>
/// Most concerns other mediators expose as runtime options — handler discovery, assembly
/// lists, behavior registration — do not exist here: the generator resolves them at compile
/// time. Only the service lifetimes remain configurable.
/// </remarks>
public sealed class QuantixOptions
{
    /// <summary>
    /// Gets or sets the <see cref="ServiceLifetime"/> with which discovered handlers are
    /// registered. The default is <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Gets or sets the <see cref="ServiceLifetime"/> with which discovered pipeline
    /// behaviors are registered. The default is <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    public ServiceLifetime BehaviorLifetime { get; set; } = ServiceLifetime.Scoped;
}
