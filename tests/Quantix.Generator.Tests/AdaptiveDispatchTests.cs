using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Quantix.Generator.Tests;

/// <summary>Tests that the generator picks its dispatch shape by message count (design section 6.3).</summary>
public class AdaptiveDispatchTests
{
    [Fact]
    public void Uses_type_pattern_routing_below_the_threshold()
    {
        GeneratorResult result = GeneratorTestHarness.Run(BuildSource(messageCount: 3));

        Assert.Contains("class QuantixMediator", result.AllGeneratedSource);
        Assert.DoesNotContain("FrozenDictionary", result.AllGeneratedSource);
    }

    [Fact]
    public void Uses_a_frozen_discriminator_map_above_the_threshold()
    {
        GeneratorResult result = GeneratorTestHarness.Run(BuildSource(messageCount: 40));

        Assert.Contains("FrozenDictionary", result.AllGeneratedSource);
        Assert.Contains("Discriminator", result.AllGeneratedSource);
    }

    /// <summary>
    /// Regression for the v1.0.1 fix. The frozen-dispatch branch used to emit
    /// <c>((ConcreteMessage)command)</c>, where <c>command</c> is the generic interface
    /// <c>ICommand&lt;TResult&gt;</c>. For a <c>sealed</c> concrete message the compiler rejects
    /// that cast as CS0030 — the bug consumers hit because <c>sealed record</c> is the
    /// idiomatic message shape. The fix casts via <c>object</c>. This test exercises the
    /// frozen path (33 messages, one above the threshold) and verifies the emitted source
    /// actually compiles.
    /// </summary>
    [Fact]
    public void Frozen_path_compiles_when_messages_are_sealed_with_generic_interfaces()
    {
        string source = BuildSource(messageCount: 33);

        // Guard the test itself: if the threshold ever climbs past 33 this assertion fails
        // loud, prompting whoever changed it to grow the message count rather than silently
        // letting this test slide off the frozen path.
        GeneratorResult runResult = GeneratorTestHarness.Run(source);
        Assert.Contains("FrozenDictionary", runResult.AllGeneratedSource);

        Compilation compilation = GeneratorTestHarness.Compile(source);
        Diagnostic[] errors = compilation
            .GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.True(
            errors.Length == 0,
            "The generated source must compile cleanly when messages are sealed. Errors:\n"
                + string.Join("\n", errors.Select(static diagnostic => diagnostic.ToString())));
    }

    /// <summary>Builds a compilation with the given number of command/handler pairs.</summary>
    private static string BuildSource(int messageCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("using Quantix;");
        builder.AppendLine("using System.Threading;");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine();
        builder.AppendLine("namespace App;");
        builder.AppendLine();

        for (int i = 0; i < messageCount; i++)
        {
            builder.AppendLine($"public sealed record Msg{i}(int Value) : ICommand<int>;");
            builder.AppendLine($"public sealed class Msg{i}Handler : ICommandHandler<Msg{i}, int>");
            builder.AppendLine("{");
            builder.AppendLine($"    public ValueTask<int> Handle(Msg{i} command, CancellationToken ct) => new(command.Value);");
            builder.AppendLine("}");
        }

        return builder.ToString();
    }
}
