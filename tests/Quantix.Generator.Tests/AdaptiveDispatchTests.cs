using System.Text;
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
