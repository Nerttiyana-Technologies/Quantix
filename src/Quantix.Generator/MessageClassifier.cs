// Classifies a candidate type as a Quantix message (design section 4.1; plan L2-A).

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Quantix.Generator;

/// <summary>
/// Inspects a type symbol and, when it implements a Quantix message marker interface, describes
/// it as a <see cref="DiscoveredMessage"/>. A type implementing more than one message interface
/// is flagged ambiguous (reported by QTX0006).
/// </summary>
internal static class MessageClassifier
{
    /// <summary>
    /// Classifies <paramref name="candidate"/> as a Quantix message, or returns null when it is
    /// not a concrete message type. The candidate may be a non-generic message type declaration
    /// or a closed instantiation of a generic message; an open-generic message declaration is
    /// never a concrete message and is skipped.
    /// </summary>
    /// <param name="candidate">The type being inspected.</param>
    /// <returns>A <see cref="DiscoveredMessage"/>, or null when the type is not a message.</returns>
    public static DiscoveredMessage? Classify(INamedTypeSymbol candidate)
    {
        // A message is a concrete (non-abstract) class or struct. An abstract base message is
        // never sent directly, so it is not required to have a handler.
        if (candidate.IsAbstract ||
            (candidate.TypeKind != TypeKind.Class && candidate.TypeKind != TypeKind.Struct))
        {
            return null;
        }

        // An open generic — the message declaration GetById<TEntity> itself — is not dispatched.
        // Only its concrete instantiations are, and those are discovered at object-creation sites.
        if (ContainsTypeParameter(candidate))
        {
            return null;
        }

        MessageKind? kind = null;
        string? resultType = null;
        int messageInterfaceCount = 0;

        foreach (INamedTypeSymbol iface in candidate.AllInterfaces)
        {
            if (MatchMessageKind(iface.OriginalDefinition) is { } matched)
            {
                messageInterfaceCount++;
                if (kind is null)
                {
                    kind = matched;

                    // ICommand<T> / IQuery<T> / IStreamRequest<T> carry the result type; the
                    // void ICommand and INotification markers carry none.
                    resultType = iface.TypeArguments.Length == 1
                        ? SymbolHelpers.ToFullyQualifiedName(iface.TypeArguments[0])
                        : null;
                }
            }
        }

        if (kind is not { } resolvedKind)
        {
            return null;
        }

        bool isGeneric = candidate.IsGenericType;
        return new DiscoveredMessage(
            resolvedKind,
            SymbolHelpers.ToFullyQualifiedName(candidate),
            resultType,
            LocationInfo.From(candidate),
            messageInterfaceCount > 1,
            CollectSatisfiedTypes(candidate),
            candidate.IsReferenceType,
            HasDefaultConstructor(candidate),
            candidate.IsUnmanagedType,
            isGeneric ? SymbolHelpers.ToUnboundGenericName(candidate) : null,
            isGeneric ? CollectTypeArguments(candidate) : EquatableArray<string>.Empty);
    }

    /// <summary>Determines whether a type is, or contains, a generic type parameter.</summary>
    private static bool ContainsTypeParameter(ITypeSymbol type)
    {
        switch (type)
        {
            case ITypeParameterSymbol:
                return true;
            case IArrayTypeSymbol array:
                return ContainsTypeParameter(array.ElementType);
            case INamedTypeSymbol named:
                foreach (ITypeSymbol argument in named.TypeArguments)
                {
                    if (ContainsTypeParameter(argument))
                    {
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }
    }

    /// <summary>Collects the closed type arguments of a constructed generic message, fully qualified.</summary>
    private static EquatableArray<string> CollectTypeArguments(INamedTypeSymbol message)
    {
        ImmutableArray<ITypeSymbol> arguments = message.TypeArguments;
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>(arguments.Length);
        foreach (ITypeSymbol argument in arguments)
        {
            builder.Add(SymbolHelpers.ToFullyQualifiedName(argument));
        }

        return new EquatableArray<string>(builder.ToImmutable());
    }

    /// <summary>
    /// Collects every type the message satisfies as a constraint: the message type itself, every
    /// base type up to (but excluding) <see cref="object"/>, and every interface it implements.
    /// A behavior's <c>where T : X</c> constraint holds exactly when the substituted <c>X</c> is
    /// in this set.
    /// </summary>
    private static EquatableArray<string> CollectSatisfiedTypes(INamedTypeSymbol message)
    {
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();
        builder.Add(SymbolHelpers.ToFullyQualifiedName(message));

        for (INamedTypeSymbol? baseType = message.BaseType;
             baseType is not null && baseType.SpecialType != SpecialType.System_Object;
             baseType = baseType.BaseType)
        {
            builder.Add(SymbolHelpers.ToFullyQualifiedName(baseType));
        }

        foreach (INamedTypeSymbol iface in message.AllInterfaces)
        {
            builder.Add(SymbolHelpers.ToFullyQualifiedName(iface));
        }

        return new EquatableArray<string>(builder.ToImmutable());
    }

    /// <summary>
    /// Determines whether the message satisfies a <c>new()</c> constraint — it has a public
    /// parameterless constructor. Every value type has one implicitly.
    /// </summary>
    private static bool HasDefaultConstructor(INamedTypeSymbol message)
    {
        if (message.IsValueType)
        {
            return true;
        }

        foreach (IMethodSymbol constructor in message.InstanceConstructors)
        {
            if (constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility == Accessibility.Public)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Maps a Quantix message marker interface to its message kind, or null.</summary>
    private static MessageKind? MatchMessageKind(INamedTypeSymbol definition)
    {
        if (SymbolHelpers.IsQuantixType(definition, "ICommand", 0))
        {
            return MessageKind.VoidCommand;
        }

        if (SymbolHelpers.IsQuantixType(definition, "ICommand", 1))
        {
            return MessageKind.Command;
        }

        if (SymbolHelpers.IsQuantixType(definition, "IQuery", 1))
        {
            return MessageKind.Query;
        }

        if (SymbolHelpers.IsQuantixType(definition, "INotification", 0))
        {
            return MessageKind.Notification;
        }

        if (SymbolHelpers.IsQuantixType(definition, "IStreamRequest", 1))
        {
            return MessageKind.StreamRequest;
        }

        return null;
    }
}
