// Shared symbol helpers for the discovery classifiers (design section 6, stage 1).

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Quantix.Generator;

/// <summary>
/// Helpers for matching and formatting Roslyn symbols during discovery. Quantix interfaces are
/// recognised by their namespace, name and generic arity — the generator resolves no well-known
/// symbols, which keeps the per-node discovery transform cheap.
/// </summary>
internal static class SymbolHelpers
{
    private static readonly SymbolDisplayFormat UnboundGenericFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    /// <summary>
    /// Determines whether <paramref name="type"/> is the Quantix type with the given name and
    /// generic arity — that is, a type of that name and arity declared directly in the global
    /// <c>Quantix</c> namespace.
    /// </summary>
    /// <param name="type">The type to test, normally an interface's original definition.</param>
    /// <param name="name">The simple type name, for example <c>ICommandHandler</c>.</param>
    /// <param name="arity">The expected generic arity.</param>
    /// <returns>True when the type is the named Quantix type.</returns>
    public static bool IsQuantixType(INamedTypeSymbol type, string name, int arity)
        => type.Arity == arity
           && type.Name == name
           && type.ContainingNamespace is { Name: "Quantix", ContainingNamespace.IsGlobalNamespace: true };

    /// <summary>Returns the fully-qualified, <c>global::</c>-prefixed name of a type.</summary>
    /// <param name="type">The type to format.</param>
    /// <returns>The fully-qualified name.</returns>
    public static string ToFullyQualifiedName(ITypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    /// <summary>Returns the fully-qualified name of a generic type without its type parameters.</summary>
    /// <param name="type">The generic type.</param>
    /// <returns>The unbound name, for example <c>global::App.LoggingBehavior</c>.</returns>
    public static string ToUnboundGenericName(INamedTypeSymbol type)
        => type.ToDisplayString(UnboundGenericFormat);

    /// <summary>
    /// Formats a behavior constraint type as a template: the fully-qualified type, with every
    /// reference to one of <paramref name="typeParameters"/> rendered as a positional placeholder
    /// — <c>{0}</c> for the first type parameter, <c>{1}</c> for the second, and so on. Model
    /// building substitutes the message and result types into the placeholders, then tests the
    /// resulting type against the message's satisfied-types set.
    /// </summary>
    /// <param name="type">The constraint type to format.</param>
    /// <param name="typeParameters">The behavior's own type parameters, in declaration order.</param>
    /// <returns>The constraint template.</returns>
    public static string FormatConstraintType(
        ITypeSymbol type,
        ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        if (type is ITypeParameterSymbol parameter)
        {
            for (int i = 0; i < typeParameters.Length; i++)
            {
                if (SymbolEqualityComparer.Default.Equals(typeParameters[i], parameter))
                {
                    return $"{{{i}}}";
                }
            }

            return parameter.Name;
        }

        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var builder = new StringBuilder();
            builder.Append(ToUnboundGenericName(named));
            builder.Append('<');

            ImmutableArray<ITypeSymbol> arguments = named.TypeArguments;
            for (int i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(FormatConstraintType(arguments[i], typeParameters));
            }

            builder.Append('>');
            return builder.ToString();
        }

        return ToFullyQualifiedName(type);
    }

    /// <summary>
    /// Reads the single integer constructor argument of a Quantix attribute applied to a type,
    /// defaulting to 0 when the attribute is absent.
    /// </summary>
    /// <param name="type">The type whose attributes to inspect.</param>
    /// <param name="attributeName">The attribute's simple name, for example <c>PipelineOrderAttribute</c>.</param>
    /// <returns>The integer argument, or 0 when the attribute is not present.</returns>
    public static int ReadAttributeIntArgument(INamedTypeSymbol type, string attributeName)
    {
        foreach (AttributeData attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass is { } attributeClass
                && IsQuantixType(attributeClass, attributeName, 0)
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is int value)
            {
                return value;
            }
        }

        return 0;
    }
}
