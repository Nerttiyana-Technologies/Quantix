using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Quantix.Generator.Tests;

/// <summary>
/// Drives <see cref="QuantixNavigationAnalyzer"/> over an in-memory C# compilation, returning
/// the diagnostics it reported for assertions.
/// </summary>
public static class AnalyzerTestHarness
{
    /// <summary>Runs the Quantix navigation analyzer against the given source text.</summary>
    /// <param name="source">The C# source to compile and analyze.</param>
    /// <returns>The diagnostics the analyzer reported.</returns>
    public static IReadOnlyList<Diagnostic> Run(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "QuantixAnalyzerTests",
            syntaxTrees: new[] { syntaxTree },
            references: GeneratorTestHarness.CollectReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new QuantixNavigationAnalyzer()));

        return withAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
    }
}
