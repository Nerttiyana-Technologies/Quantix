// Classifies a candidate type as a Quantix pipeline behavior (design section 4.4; plan L2-A).

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Quantix.Generator;

/// <summary>
/// Inspects a type symbol and, when it implements a Quantix pipeline behavior interface,
/// describes it as a <see cref="DiscoveredBehavior"/>.
/// </summary>
internal static class BehaviorClassifier
{
    /// <summary>
    /// Classifies <paramref name="candidate"/> as a Quantix pipeline behavior, or returns null
    /// when it does not implement a behavior interface.
    /// </summary>
    /// <param name="candidate">The type being inspected.</param>
    /// <returns>A <see cref="DiscoveredBehavior"/>, or null when the type is not a behavior.</returns>
    public static DiscoveredBehavior? Classify(INamedTypeSymbol candidate)
    {
        if (candidate.TypeKind != TypeKind.Class)
        {
            return null;
        }

        foreach (INamedTypeSymbol iface in candidate.AllInterfaces)
        {
            INamedTypeSymbol definition = iface.OriginalDefinition;

            if (SymbolHelpers.IsQuantixType(definition, "IPipelineBehavior", 2))
            {
                return Describe(BehaviorKind.Request, candidate, iface, hasResult: true);
            }

            if (SymbolHelpers.IsQuantixType(definition, "ICommandPipelineBehavior", 1))
            {
                return Describe(BehaviorKind.Command, candidate, iface, hasResult: false);
            }

            if (SymbolHelpers.IsQuantixType(definition, "IStreamPipelineBehavior", 2))
            {
                return Describe(BehaviorKind.Stream, candidate, iface, hasResult: true);
            }
        }

        return null;
    }

    /// <summary>Builds a <see cref="DiscoveredBehavior"/> from a matched behavior interface.</summary>
    private static DiscoveredBehavior Describe(
        BehaviorKind kind,
        INamedTypeSymbol candidate,
        INamedTypeSymbol behaviorInterface,
        bool hasResult)
    {
        bool isOpenGeneric = candidate.IsGenericType;
        bool isSimpleShape = isOpenGeneric && IsSimpleShape(candidate, behaviorInterface);

        return new DiscoveredBehavior(
            kind,
            SymbolHelpers.ToFullyQualifiedName(candidate),
            isOpenGeneric,
            SymbolHelpers.ReadAttributeIntArgument(candidate, "PipelineOrderAttribute"),
            isOpenGeneric ? null : SymbolHelpers.ToFullyQualifiedName(behaviorInterface.TypeArguments[0]),
            isOpenGeneric || !hasResult ? null : SymbolHelpers.ToFullyQualifiedName(behaviorInterface.TypeArguments[1]),
            LocationInfo.From(candidate),
            isSimpleShape,
            isOpenGeneric ? SymbolHelpers.ToUnboundGenericName(candidate) : null,
            isSimpleShape ? BuildConstraints(candidate) : null);
    }

    /// <summary>
    /// Determines whether an open-generic behavior uses the simple shape — its interface type
    /// arguments are exactly its own type parameters, in order — so it can be closed positionally
    /// over a message. Only simple-shape open generics are applied; an exotic shape such as
    /// <c>Behavior&lt;T&gt; : IPipelineBehavior&lt;Wrapper&lt;T&gt;, T&gt;</c> cannot be closed
    /// from a message and result type alone, and is not discovered as an open generic.
    /// </summary>
    private static bool IsSimpleShape(INamedTypeSymbol candidate, INamedTypeSymbol behaviorInterface)
    {
        ImmutableArray<ITypeSymbol> interfaceArguments = behaviorInterface.TypeArguments;
        ImmutableArray<ITypeParameterSymbol> typeParameters = candidate.TypeParameters;
        if (interfaceArguments.Length != typeParameters.Length)
        {
            return false;
        }

        for (int i = 0; i < interfaceArguments.Length; i++)
        {
            if (!SymbolEqualityComparer.Default.Equals(interfaceArguments[i], typeParameters[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Captures the generic constraints of a simple-shape open-generic behavior. The special and
    /// type constraints of the first type parameter — the message type — are recorded so model
    /// building can scope the behavior; a constraint on any later type parameter is only flagged,
    /// since Quantix v1 does not evaluate result-type-parameter constraints.
    /// </summary>
    private static BehaviorConstraints BuildConstraints(INamedTypeSymbol candidate)
    {
        ImmutableArray<ITypeParameterSymbol> typeParameters = candidate.TypeParameters;
        ITypeParameterSymbol first = typeParameters[0];

        ImmutableArray<string>.Builder typeConstraints =
            ImmutableArray.CreateBuilder<string>(first.ConstraintTypes.Length);
        foreach (ITypeSymbol constraintType in first.ConstraintTypes)
        {
            typeConstraints.Add(SymbolHelpers.FormatConstraintType(constraintType, typeParameters));
        }

        bool hasConstraintsBeyondFirst = false;
        for (int i = 1; i < typeParameters.Length; i++)
        {
            if (HasAnyConstraint(typeParameters[i]))
            {
                hasConstraintsBeyondFirst = true;
                break;
            }
        }

        return new BehaviorConstraints(
            first.HasReferenceTypeConstraint,
            first.HasValueTypeConstraint,
            first.HasConstructorConstraint,
            first.HasUnmanagedTypeConstraint,
            new EquatableArray<string>(typeConstraints.ToImmutable()),
            hasConstraintsBeyondFirst);
    }

    /// <summary>Determines whether a type parameter carries any generic constraint.</summary>
    private static bool HasAnyConstraint(ITypeParameterSymbol parameter)
        => parameter.HasReferenceTypeConstraint
           || parameter.HasValueTypeConstraint
           || parameter.HasConstructorConstraint
           || parameter.HasNotNullConstraint
           || parameter.HasUnmanagedTypeConstraint
           || parameter.ConstraintTypes.Length > 0;
}
