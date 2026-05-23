// Classifies a candidate type as a Quantix handler (design section 6, stage 1; plan L2-A3).

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Quantix.Generator;

/// <summary>
/// Inspects a type symbol and, when it implements a Quantix handler interface, describes it as
/// a <see cref="DiscoveredHandler"/>.
/// </summary>
internal static class HandlerClassifier
{
    /// <summary>
    /// Classifies <paramref name="candidate"/> as a Quantix handler, or returns null when it
    /// does not implement a handler interface. Abstract classes are still classified so the
    /// validation stage can report diagnostic QTX0003.
    /// </summary>
    /// <param name="candidate">The type being inspected.</param>
    /// <returns>A <see cref="DiscoveredHandler"/>, or null when the type is not a handler.</returns>
    public static DiscoveredHandler? Classify(INamedTypeSymbol candidate)
    {
        // Only classes (including record classes) can be handlers — the container constructs them.
        if (candidate.TypeKind != TypeKind.Class)
        {
            return null;
        }

        foreach (INamedTypeSymbol iface in candidate.AllInterfaces)
        {
            INamedTypeSymbol definition = iface.OriginalDefinition;

            if (SymbolHelpers.IsQuantixType(definition, "ICommandHandler", 1))
            {
                return Describe(MessageKind.VoidCommand, candidate, iface, hasResult: false);
            }

            if (SymbolHelpers.IsQuantixType(definition, "ICommandHandler", 2))
            {
                return Describe(MessageKind.Command, candidate, iface, hasResult: true);
            }

            if (SymbolHelpers.IsQuantixType(definition, "IQueryHandler", 2))
            {
                return Describe(MessageKind.Query, candidate, iface, hasResult: true);
            }

            if (SymbolHelpers.IsQuantixType(definition, "INotificationHandler", 1))
            {
                return Describe(MessageKind.Notification, candidate, iface, hasResult: false);
            }

            if (SymbolHelpers.IsQuantixType(definition, "IStreamRequestHandler", 2))
            {
                return Describe(MessageKind.StreamRequest, candidate, iface, hasResult: true);
            }
        }

        return null;
    }

    /// <summary>
    /// Builds a <see cref="DiscoveredHandler"/> from a matched handler interface. Returns null
    /// for an open-generic handler whose shape cannot be closed positionally over a message.
    /// </summary>
    private static DiscoveredHandler? Describe(
        MessageKind kind,
        INamedTypeSymbol candidate,
        INamedTypeSymbol handlerInterface,
        bool hasResult)
    {
        ITypeSymbol messageTypeArgument = handlerInterface.TypeArguments[0];

        if (candidate.IsGenericType)
        {
            // An open-generic handler for a generic message — closed over every concrete
            // instantiation in model building. Only the simple shape, where the message's type
            // arguments are exactly the handler's type parameters in order, can be closed.
            if (messageTypeArgument is not INamedTypeSymbol { IsGenericType: true } openMessage
                || !IsSimpleShapeHandler(candidate, openMessage))
            {
                return null;
            }

            return new DiscoveredHandler(
                kind,
                SymbolHelpers.ToFullyQualifiedName(candidate),
                SymbolHelpers.ToFullyQualifiedName(messageTypeArgument),
                hasResult ? SymbolHelpers.ToFullyQualifiedName(handlerInterface.TypeArguments[1]) : null,
                candidate.IsAbstract,
                LocationInfo.From(candidate),
                SymbolHelpers.ReadAttributeIntArgument(candidate, "NotificationOrderAttribute"),
                HasSignatureMismatch(candidate, handlerInterface),
                IsOpenGeneric: true,
                SymbolHelpers.ToUnboundGenericName(candidate),
                SymbolHelpers.ToUnboundGenericName(openMessage));
        }

        return new DiscoveredHandler(
            kind,
            SymbolHelpers.ToFullyQualifiedName(candidate),
            SymbolHelpers.ToFullyQualifiedName(messageTypeArgument),
            hasResult ? SymbolHelpers.ToFullyQualifiedName(handlerInterface.TypeArguments[1]) : null,
            candidate.IsAbstract,
            LocationInfo.From(candidate),
            SymbolHelpers.ReadAttributeIntArgument(candidate, "NotificationOrderAttribute"),
            HasSignatureMismatch(candidate, handlerInterface),
            IsOpenGeneric: false,
            OpenHandlerGenericName: null,
            OpenMessageGenericName: null);
    }

    /// <summary>
    /// Determines whether an open-generic handler uses the simple shape — the generic message it
    /// handles is closed over exactly the handler's own type parameters, in declaration order —
    /// so the handler can be closed positionally over a concrete instantiation of the message.
    /// </summary>
    private static bool IsSimpleShapeHandler(INamedTypeSymbol handler, INamedTypeSymbol openMessage)
    {
        ImmutableArray<ITypeParameterSymbol> typeParameters = handler.TypeParameters;
        ImmutableArray<ITypeSymbol> messageArguments = openMessage.TypeArguments;
        if (typeParameters.Length == 0 || messageArguments.Length != typeParameters.Length)
        {
            return false;
        }

        for (int i = 0; i < typeParameters.Length; i++)
        {
            if (!SymbolEqualityComparer.Default.Equals(messageArguments[i], typeParameters[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the candidate declares the handler interface but leaves its
    /// <c>Handle</c> method unimplemented — the type has the wrong return type or parameters, so
    /// no member satisfies the interface. An abstract handler is excluded: QTX0003 already covers
    /// it, and an abstract type legitimately leaves interface members unimplemented.
    /// </summary>
    private static bool HasSignatureMismatch(INamedTypeSymbol candidate, INamedTypeSymbol handlerInterface)
    {
        if (candidate.IsAbstract)
        {
            return false;
        }

        foreach (ISymbol member in handlerInterface.GetMembers("Handle"))
        {
            if (member is IMethodSymbol handleMethod)
            {
                return candidate.FindImplementationForInterfaceMember(handleMethod) is null;
            }
        }

        return false;
    }
}
