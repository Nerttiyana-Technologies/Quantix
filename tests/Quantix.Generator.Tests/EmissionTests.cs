using Xunit;

namespace Quantix.Generator.Tests;

/// <summary>Tests asserting the source the generator emits.</summary>
public class EmissionTests
{
    [Fact]
    public void Generates_the_mediator_and_registration_for_a_command_handler()
    {
        const string source = """
            using Quantix;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App;

            public sealed record Greet(string Name) : ICommand<string>;

            public sealed class GreetHandler : ICommandHandler<Greet, string>
            {
                public ValueTask<string> Handle(Greet command, CancellationToken ct) => new("hi");
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.Empty(result.Diagnostics);
        Assert.Contains("class QuantixMediator", result.AllGeneratedSource);
        Assert.Contains("AddQuantix", result.AllGeneratedSource);
        Assert.Contains("global::App.GreetHandler", result.AllGeneratedSource);
    }

    [Fact]
    public void Generates_nothing_when_the_compilation_uses_no_quantix_types()
    {
        const string source = """
            namespace App;

            public sealed class Plain
            {
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.Empty(result.GeneratedHintNames);
        Assert.Empty(result.Diagnostics);
    }
}
