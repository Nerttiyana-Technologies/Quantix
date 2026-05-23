// Quantix.Generator — the incremental source generator entry point (design section 6).

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Quantix.Generator;

/// <summary>
/// The Quantix incremental source generator. It discovers messages, handlers and pipeline
/// behaviors at compile time and emits the mediator dispatcher and the dependency-injection
/// registration into the consuming assembly — with no runtime reflection.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class QuantixGenerator : IIncrementalGenerator
{
    private const string EmitManifestProperty = "build_property.QuantixEmitManifest";
    private const string ReportDiscoveryProperty = "build_property.QuantixReportDiscovery";

    /// <summary>
    /// Wires up the generator's incremental pipeline: discovery, model building, validation and
    /// emission, plus the opt-in pipeline manifest and discovery reporting.
    /// </summary>
    /// <param name="context">The initialization context supplied by the Roslyn host.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<DiscoveryResult> discovered = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateType(node),
                transform: static (syntaxContext, cancellationToken) => Transform(syntaxContext, cancellationToken))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);

        // A second syntax provider collects concrete instantiations of generic messages from
        // object-creation sites — every `new GetById<Customer>(...)` in the compilation.
        IncrementalValuesProvider<DiscoveredMessage> instantiations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsGenericConstruction(node),
                transform: static (syntaxContext, cancellationToken) => TransformInstantiation(syntaxContext, cancellationToken))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);

        IncrementalValueProvider<bool> reportDiscovery = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => IsPropertyEnabled(options, ReportDiscoveryProperty));

        IncrementalValueProvider<QuantixModel> model = discovered
            .Collect()
            .Combine(instantiations.Collect())
            .Combine(reportDiscovery)
            .Select(static (pair, _) => ModelBuilder.Build(pair.Left.Left, pair.Left.Right, pair.Right));

        IncrementalValueProvider<bool> emitManifest = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => IsPropertyEnabled(options, EmitManifestProperty));

        context.RegisterSourceOutput(
            model.Combine(emitManifest),
            static (productionContext, pair) => EmitOutputs(productionContext, pair.Left, pair.Right));
    }

    /// <summary>The cheap syntactic pre-filter: a type declaration that carries a base list.</summary>
    private static bool IsCandidateType(SyntaxNode node)
        => node is TypeDeclarationSyntax { BaseList: not null };

    /// <summary>
    /// The semantic transform: resolves the candidate's symbol and classifies it as a Quantix
    /// handler, pipeline behavior or message. Returns null when the type is none of those.
    /// </summary>
    private static DiscoveryResult? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var declaration = (TypeDeclarationSyntax)context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type)
        {
            return null;
        }

        // Quantix interfaces are matched by namespace, name and arity — the generator resolves
        // no well-known symbols, so this per-node transform stays cheap.
        DiscoveredHandler? handler = HandlerClassifier.Classify(type);
        if (handler is not null)
        {
            return new DiscoveryResult(handler, null, null);
        }

        DiscoveredBehavior? behavior = BehaviorClassifier.Classify(type);
        if (behavior is not null)
        {
            return new DiscoveryResult(null, behavior, null);
        }

        DiscoveredMessage? message = MessageClassifier.Classify(type);
        if (message is not null)
        {
            return new DiscoveryResult(null, null, message);
        }

        return null;
    }

    /// <summary>The cheap syntactic pre-filter: an object creation of a generic type.</summary>
    private static bool IsGenericConstruction(SyntaxNode node)
        => node is ObjectCreationExpressionSyntax creation && IsGenericTypeName(creation.Type);

    /// <summary>Determines whether a type syntax names a generic type, including a qualified one.</summary>
    private static bool IsGenericTypeName(TypeSyntax type)
        => type switch
        {
            GenericNameSyntax => true,
            QualifiedNameSyntax qualified => qualified.Right is GenericNameSyntax,
            AliasQualifiedNameSyntax alias => alias.Name is GenericNameSyntax,
            _ => false,
        };

    /// <summary>
    /// The semantic transform for an object-creation site: resolves the constructed type and,
    /// when it is a concrete instantiation of a generic Quantix message, describes it. Returns
    /// null otherwise.
    /// </summary>
    private static DiscoveredMessage? TransformInstantiation(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetTypeInfo(creation, cancellationToken).Type is not INamedTypeSymbol type)
        {
            return null;
        }

        // MessageClassifier skips open generics and non-messages; only a concrete instantiation
        // of a generic message carries an OpenGenericName.
        DiscoveredMessage? message = MessageClassifier.Classify(type);
        return message is { OpenGenericName: not null } ? message : null;
    }

    /// <summary>Reads whether the named opt-in MSBuild boolean property is set to <c>true</c>.</summary>
    private static bool IsPropertyEnabled(AnalyzerConfigOptionsProvider options, string propertyName)
        => options.GlobalOptions.TryGetValue(propertyName, out string? value)
           && string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase);

    /// <summary>Reports diagnostics and emits the generated source for one compilation.</summary>
    private static void EmitOutputs(SourceProductionContext context, QuantixModel model, bool emitManifest)
    {
        foreach (DiagnosticInfo diagnostic in model.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
        }

        if (model.Messages.Count == 0)
        {
            return;
        }

        context.AddSource("QuantixMediator.g.cs", MediatorEmitter.Emit(model));
        context.AddSource("QuantixRegistration.g.cs", RegistrationEmitter.Emit(model));

        if (emitManifest)
        {
            context.AddSource("Quantix.Manifest.g.cs", ManifestEmitter.Emit(model));
        }
    }
}
