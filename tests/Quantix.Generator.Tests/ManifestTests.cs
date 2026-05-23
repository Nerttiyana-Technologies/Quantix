using Xunit;

namespace Quantix.Generator.Tests;

/// <summary>
/// Tests for the opt-in pipeline manifest (design section 6.2): it is silent by default and,
/// when <c>QuantixEmitManifest</c> is set, emits a comment-only map of every message.
/// </summary>
public class ManifestTests
{
    private const string ManifestHintName = "Quantix.Manifest.g.cs";
    private const string EmitManifestProperty = "build_property.QuantixEmitManifest";

    [Fact]
    public void Manifest_is_not_emitted_by_default()
    {
        GeneratorResult result = GeneratorTestHarness.Run(CommandSource);

        Assert.False(result.HasGeneratedFile(ManifestHintName));
    }

    [Fact]
    public void Manifest_is_not_emitted_when_the_property_is_false()
    {
        GeneratorResult result = GeneratorTestHarness.Run(
            CommandSource,
            new Dictionary<string, string> { [EmitManifestProperty] = "false" });

        Assert.False(result.HasGeneratedFile(ManifestHintName));
    }

    [Fact]
    public void Manifest_maps_a_command_to_its_handler_when_enabled()
    {
        GeneratorResult result = GeneratorTestHarness.Run(
            CommandSource,
            new Dictionary<string, string> { [EmitManifestProperty] = "true" });

        Assert.True(result.HasGeneratedFile(ManifestHintName));

        string manifest = result.GetGeneratedFile(ManifestHintName);
        Assert.Contains("Quantix pipeline manifest", manifest);
        Assert.Contains("1 message(s) discovered", manifest);
        Assert.Contains("global::App.Greet", manifest);
        Assert.Contains("kind: command -> string", manifest);
        Assert.Contains("handler: global::App.GreetHandler", manifest);
    }

    [Fact]
    public void Manifest_lists_the_behavior_pipeline()
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

            public sealed class TimingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
            {
                public ValueTask<TResult> Handle(
                    TRequest request,
                    RequestHandlerDelegate<TResult> next,
                    CancellationToken ct) => next(ct);
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(
            source,
            new Dictionary<string, string> { [EmitManifestProperty] = "true" });

        string manifest = result.GetGeneratedFile(ManifestHintName);
        Assert.Contains("pipeline (outermost first):", manifest);
        Assert.Contains("TimingBehavior", manifest);
    }

    [Fact]
    public void Manifest_lists_every_notification_handler()
    {
        const string source = """
            using Quantix;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App;

            public sealed record Pinged : INotification;

            public sealed class FirstHandler : INotificationHandler<Pinged>
            {
                public ValueTask Handle(Pinged notification, CancellationToken ct) => default;
            }

            public sealed class SecondHandler : INotificationHandler<Pinged>
            {
                public ValueTask Handle(Pinged notification, CancellationToken ct) => default;
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(
            source,
            new Dictionary<string, string> { [EmitManifestProperty] = "true" });

        string manifest = result.GetGeneratedFile(ManifestHintName);
        Assert.Contains("kind: notification", manifest);
        Assert.Contains("handlers: 2", manifest);
        Assert.Contains("global::App.FirstHandler", manifest);
        Assert.Contains("global::App.SecondHandler", manifest);
    }

    /// <summary>A single command and its handler — the minimal manifest fixture.</summary>
    private const string CommandSource = """
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
}
