// The Quantix navigation analyzer — the handler hint (design section 6.1, D12; plan L2-H).

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Quantix.Generator;

/// <summary>
/// The Quantix navigation analyzer. It ships in the same package as the generator and ends the
/// MediatR "blindfold": at every <c>Send</c>, <c>Publish</c> or <c>Stream</c> call site it
/// reports the informational diagnostic QTX0012 naming the handler — or, for a notification,
/// the handlers — that the generated dispatcher runs for that message. The diagnostic is
/// <see cref="DiagnosticSeverity.Info"/>, so it never affects a build; it answers the question
/// "what actually runs when I send this?" directly at the call.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class QuantixNavigationAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableArray<DiagnosticDescriptor> Diagnostics =
        ImmutableArray.Create(QuantixDiagnostics.HandlerHint);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics;

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // The handler map is built once per compilation, then read concurrently per call site.
        context.RegisterCompilationStartAction(static startContext =>
        {
            Dictionary<string, List<DiscoveredHandler>> handlersByMessage =
                BuildHandlerMap(startContext.Compilation);

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeInvocation(nodeContext, handlersByMessage),
                SyntaxKind.InvocationExpression);
        });
    }

    /// <summary>
    /// Builds the message-to-handlers map for the compilation by classifying every type it
    /// declares. Open-generic handlers are keyed by the parameterless name of their generic
    /// message; every handler list is sorted so the hint is stable.
    /// </summary>
    private static Dictionary<string, List<DiscoveredHandler>> BuildHandlerMap(Compilation compilation)
    {
        var map = new Dictionary<string, List<DiscoveredHandler>>(StringComparer.Ordinal);

        foreach (INamedTypeSymbol type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
        {
            if (HandlerClassifier.Classify(type) is not { } handler)
            {
                continue;
            }

            string key = handler is { IsOpenGeneric: true, OpenMessageGenericName: { } openMessage }
                ? openMessage
                : handler.MessageType;

            if (!map.TryGetValue(key, out List<DiscoveredHandler>? handlers))
            {
                handlers = new List<DiscoveredHandler>();
                map.Add(key, handlers);
            }

            handlers.Add(handler);
        }

        foreach (List<DiscoveredHandler> handlers in map.Values)
        {
            handlers.Sort(static (left, right) => string.CompareOrdinal(left.HandlerType, right.HandlerType));
        }

        return map;
    }

    /// <summary>Enumerates every named type declared in a namespace, including nested types.</summary>
    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol root)
    {
        var pending = new Stack<INamespaceOrTypeSymbol>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            foreach (ISymbol member in pending.Pop().GetMembers())
            {
                if (member is INamespaceSymbol nestedNamespace)
                {
                    pending.Push(nestedNamespace);
                }
                else if (member is INamedTypeSymbol type)
                {
                    yield return type;
                    pending.Push(type);
                }
            }
        }
    }

    /// <summary>
    /// Inspects one invocation: when it is a Quantix <c>Send</c>, <c>Publish</c> or <c>Stream</c>
    /// call over a concrete message that has a handler, reports the QTX0012 hint.
    /// </summary>
    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        Dictionary<string, List<DiscoveredHandler>> handlersByMessage)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess
            || !IsMediatorVerb(memberAccess.Name.Identifier.ValueText)
            || invocation.ArgumentList is not { Arguments.Count: > 0 } argumentList)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
                is not IMethodSymbol { ContainingType: { } containingType } method
            || !SymbolHelpers.IsQuantixType(containingType, "IMediator", 0))
        {
            return;
        }

        ExpressionSyntax messageArgument = argumentList.Arguments[0].Expression;
        if (context.SemanticModel.GetTypeInfo(messageArgument, context.CancellationToken).Type
                is not INamedTypeSymbol messageType)
        {
            return;
        }

        string lookupKey = messageType.IsGenericType
            ? SymbolHelpers.ToUnboundGenericName(messageType)
            : SymbolHelpers.ToFullyQualifiedName(messageType);

        if (!handlersByMessage.TryGetValue(lookupKey, out List<DiscoveredHandler>? handlers)
            || handlers.Count == 0)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            QuantixDiagnostics.HandlerHint,
            memberAccess.Name.GetLocation(),
            messageType.Name,
            DescribeHandlers(handlers)));
    }

    /// <summary>Determines whether a method name is one of the three Quantix dispatch verbs.</summary>
    private static bool IsMediatorVerb(string name)
        => name is "Send" or "Publish" or "Stream";

    /// <summary>Formats the discovered handlers as a quoted, comma-separated list of simple names.</summary>
    private static string DescribeHandlers(List<DiscoveredHandler> handlers)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < handlers.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append('\'').Append(SimpleName(handlers[i].HandlerType)).Append('\'');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Extracts the simple type name from a fully-qualified name, keeping any generic arguments —
    /// for example <c>global::App.GetByIdHandler&lt;TEntity&gt;</c> becomes
    /// <c>GetByIdHandler&lt;TEntity&gt;</c>.
    /// </summary>
    private static string SimpleName(string fullyQualifiedName)
    {
        int generic = fullyQualifiedName.IndexOf('<');
        int boundary = generic >= 0 ? generic : fullyQualifiedName.Length;
        int lastDot = fullyQualifiedName.LastIndexOf('.', boundary - 1);
        return lastDot >= 0 ? fullyQualifiedName.Substring(lastDot + 1) : fullyQualifiedName;
    }
}
