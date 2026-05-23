using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Quantix.Generator.Tests;

/// <summary>
/// Drives <see cref="QuantixGenerator"/> over an in-memory C# compilation, returning the
/// generated source and reported diagnostics for assertions.
/// </summary>
public static class GeneratorTestHarness
{
    /// <summary>Runs the Quantix generator against the given source text.</summary>
    /// <param name="source">The C# source to compile and run the generator over.</param>
    /// <param name="globalOptions">
    /// Optional MSBuild global options (the <c>build_property.*</c> analyzer-config values) to
    /// surface to the generator, for example <c>build_property.QuantixEmitManifest</c>.
    /// </param>
    /// <returns>The generated sources and the diagnostics the generator reported.</returns>
    public static GeneratorResult Run(
        string source,
        IReadOnlyDictionary<string, string>? globalOptions = null)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "QuantixGeneratorTests",
            syntaxTrees: new[] { syntaxTree },
            references: CollectReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { new QuantixGenerator().AsSourceGenerator() },
            additionalTexts: null,
            parseOptions: null,
            optionsProvider: globalOptions is null ? null : new TestOptionsProvider(globalOptions));

        GeneratorDriverRunResult runResult = driver.RunGenerators(compilation).GetRunResult();

        var sources = new List<GeneratedFile>();
        foreach (GeneratorRunResult generatorResult in runResult.Results)
        {
            foreach (GeneratedSourceResult generated in generatorResult.GeneratedSources)
            {
                sources.Add(new GeneratedFile(generated.HintName, generated.SourceText.ToString()));
            }
        }

        return new GeneratorResult(sources, runResult.Diagnostics);
    }

    /// <summary>
    /// Collects metadata references for a test compilation: the base class library (every
    /// assembly in the running runtime's directory) plus the Quantix abstractions, located via
    /// known types so the assemblies are genuinely loaded.
    /// </summary>
    internal static List<MetadataReference> CollectReferences()
    {
        var references = new List<MetadataReference>();

        string runtimeDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        foreach (string assemblyPath in Directory.GetFiles(runtimeDirectory, "*.dll"))
        {
            references.Add(MetadataReference.CreateFromFile(assemblyPath));
        }

        references.Add(MetadataReference.CreateFromFile(typeof(global::Quantix.ICommand).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(
            typeof(global::Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location));

        return references;
    }

    /// <summary>An <see cref="AnalyzerConfigOptionsProvider"/> that surfaces fixed global options.</summary>
    private sealed class TestOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly TestOptions _global;

        /// <summary>Creates a provider over the given global MSBuild options.</summary>
        /// <param name="globalOptions">The global analyzer-config values to expose.</param>
        public TestOptionsProvider(IReadOnlyDictionary<string, string> globalOptions)
            => _global = new TestOptions(globalOptions);

        /// <inheritdoc />
        public override AnalyzerConfigOptions GlobalOptions => _global;

        /// <inheritdoc />
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestOptions.Empty;

        /// <inheritdoc />
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestOptions.Empty;
    }

    /// <summary>An <see cref="AnalyzerConfigOptions"/> backed by a fixed dictionary.</summary>
    private sealed class TestOptions : AnalyzerConfigOptions
    {
        /// <summary>An options set with no values.</summary>
        public static readonly TestOptions Empty = new(ImmutableDictionary<string, string>.Empty);

        private readonly IReadOnlyDictionary<string, string> _values;

        /// <summary>Creates an options set over the given values.</summary>
        /// <param name="values">The analyzer-config key/value pairs.</param>
        public TestOptions(IReadOnlyDictionary<string, string> values) => _values = values;

        /// <inheritdoc />
        public override bool TryGetValue(string key, out string value)
        {
            if (_values.TryGetValue(key, out string? found))
            {
                value = found;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}

/// <summary>A single source file produced by the generator.</summary>
/// <param name="HintName">The generator-assigned hint name, for example <c>QuantixMediator.g.cs</c>.</param>
/// <param name="Source">The generated source text.</param>
public readonly record struct GeneratedFile(string HintName, string Source);

/// <summary>The outcome of running the Quantix generator over a test compilation.</summary>
public sealed class GeneratorResult
{
    private readonly IReadOnlyList<GeneratedFile> _generatedFiles;

    /// <summary>Creates a result.</summary>
    /// <param name="generatedFiles">Every generated source file.</param>
    /// <param name="diagnostics">The diagnostics the generator reported.</param>
    public GeneratorResult(IReadOnlyList<GeneratedFile> generatedFiles, IReadOnlyList<Diagnostic> diagnostics)
    {
        _generatedFiles = generatedFiles;
        Diagnostics = diagnostics;
    }

    /// <summary>The diagnostics the generator reported.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>The hint names of every generated source file.</summary>
    public IEnumerable<string> GeneratedHintNames
    {
        get
        {
            foreach (GeneratedFile file in _generatedFiles)
            {
                yield return file.HintName;
            }
        }
    }

    /// <summary>Every generated source concatenated, for substring assertions.</summary>
    public string AllGeneratedSource
    {
        get
        {
            var builder = new System.Text.StringBuilder();
            foreach (GeneratedFile file in _generatedFiles)
            {
                builder.Append(file.Source).Append('\n');
            }

            return builder.ToString();
        }
    }

    /// <summary>Determines whether the generator emitted a file with the given hint name.</summary>
    /// <param name="hintName">The hint name, for example <c>Quantix.Manifest.g.cs</c>.</param>
    /// <returns>True when a file with that hint name was emitted.</returns>
    public bool HasGeneratedFile(string hintName)
        => FindGeneratedFile(hintName) is not null;

    /// <summary>Returns the source of the generated file with the given hint name.</summary>
    /// <param name="hintName">The hint name, for example <c>Quantix.Manifest.g.cs</c>.</param>
    /// <returns>The generated source text.</returns>
    /// <exception cref="InvalidOperationException">No file with that hint name was emitted.</exception>
    public string GetGeneratedFile(string hintName)
        => FindGeneratedFile(hintName)
           ?? throw new InvalidOperationException($"No generated file named '{hintName}' was emitted.");

    /// <summary>Determines whether the generator reported a diagnostic with the given id.</summary>
    /// <param name="id">The diagnostic id, for example <c>QTX0001</c>.</param>
    /// <returns>True when a diagnostic with that id was reported.</returns>
    public bool HasDiagnostic(string id)
    {
        foreach (Diagnostic diagnostic in Diagnostics)
        {
            if (diagnostic.Id == id)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the source of the named generated file, or null when it was not emitted.</summary>
    private string? FindGeneratedFile(string hintName)
    {
        foreach (GeneratedFile file in _generatedFiles)
        {
            if (file.HintName == hintName)
            {
                return file.Source;
            }
        }

        return null;
    }
}
