// Equatable location and diagnostic data carried through the generator pipeline (plan L2-C).

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Quantix.Generator;

/// <summary>
/// An equatable description of a source location. <see cref="Location"/> itself holds a
/// reference to a syntax tree and cannot be cached, so discovery stores this instead and
/// rebuilds the <see cref="Location"/> only when a diagnostic is reported.
/// </summary>
/// <param name="FilePath">The source file path.</param>
/// <param name="Span">The character span within the file.</param>
/// <param name="LineSpan">The line/character span within the file.</param>
internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
    /// <summary>Rebuilds a Roslyn <see cref="Location"/> from this information.</summary>
    /// <returns>The reconstructed location.</returns>
    public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);

    /// <summary>Captures the location of a symbol's first source declaration, if it has one.</summary>
    /// <param name="symbol">The symbol whose location to capture.</param>
    /// <returns>The location info, or null when the symbol has no source location.</returns>
    public static LocationInfo? From(ISymbol symbol)
    {
        foreach (Location location in symbol.Locations)
        {
            if (location.SourceTree is not null)
            {
                return new LocationInfo(
                    location.SourceTree.FilePath,
                    location.SourceSpan,
                    location.GetLineSpan().Span);
            }
        }

        return null;
    }
}

/// <summary>
/// An equatable description of a diagnostic to report — its descriptor, location and message
/// arguments — produced by the validation stage and converted to a <see cref="Diagnostic"/>
/// when the generator reports it.
/// </summary>
/// <param name="Descriptor">The diagnostic descriptor.</param>
/// <param name="SourceLocation">The source location, or null to report without one.</param>
/// <param name="MessageArguments">The arguments substituted into the descriptor's message.</param>
internal sealed record DiagnosticInfo(
    DiagnosticDescriptor Descriptor,
    LocationInfo? SourceLocation,
    EquatableArray<string> MessageArguments)
{
    /// <summary>Converts this information into a Roslyn <see cref="Diagnostic"/>.</summary>
    /// <returns>The diagnostic.</returns>
    public Diagnostic ToDiagnostic()
        => Diagnostic.Create(
            Descriptor,
            SourceLocation?.ToLocation() ?? Location.None,
            MessageArguments.AsImmutableArray().ToArray());
}
