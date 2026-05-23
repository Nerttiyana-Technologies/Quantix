using Xunit;

namespace Quantix.Generator.Tests;

/// <summary>Tests asserting the QTX diagnostics the generator reports.</summary>
public class DiagnosticTests
{
    [Fact]
    public void Reports_QTX0001_when_a_command_has_no_handler()
    {
        const string source = """
            using Quantix;

            namespace App;

            public sealed record Orphan : ICommand<int>;
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.True(result.HasDiagnostic("QTX0001"));
    }

    [Fact]
    public void Reports_QTX0002_when_a_command_has_two_handlers()
    {
        const string source = """
            using Quantix;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App;

            public sealed record Dup : ICommand<int>;

            public sealed class HandlerA : ICommandHandler<Dup, int>
            {
                public ValueTask<int> Handle(Dup command, CancellationToken ct) => new(1);
            }

            public sealed class HandlerB : ICommandHandler<Dup, int>
            {
                public ValueTask<int> Handle(Dup command, CancellationToken ct) => new(2);
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.True(result.HasDiagnostic("QTX0002"));
    }

    [Fact]
    public void Reports_QTX0003_for_an_abstract_handler()
    {
        const string source = """
            using Quantix;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App;

            public sealed record Cmd : ICommand<int>;

            public abstract class AbstractHandler : ICommandHandler<Cmd, int>
            {
                public abstract ValueTask<int> Handle(Cmd command, CancellationToken ct);
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.True(result.HasDiagnostic("QTX0003"));
    }

    [Fact]
    public void Reports_QTX0005_for_a_notification_with_no_handlers()
    {
        const string source = """
            using Quantix;

            namespace App;

            public sealed record Ping : INotification;
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.True(result.HasDiagnostic("QTX0005"));
    }

    [Fact]
    public void Reports_QTX0008_when_a_handler_does_not_implement_Handle()
    {
        const string source = """
            using Quantix;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App;

            public sealed record Greet(string Name) : ICommand<string>;

            public sealed class GreetHandler : ICommandHandler<Greet, string>
            {
                // Returns Task instead of ValueTask: no member satisfies the handler interface.
                public Task<string> Handle(Greet command, CancellationToken ct) => Task.FromResult("hi");
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.True(result.HasDiagnostic("QTX0008"));
    }

    [Fact]
    public void Does_not_report_QTX0010_discovery_info_by_default()
    {
        GeneratorResult result = GeneratorTestHarness.Run(DiscoverySource);

        Assert.False(result.HasDiagnostic("QTX0010"));
    }

    [Fact]
    public void Reports_QTX0010_for_each_handler_and_behavior_when_enabled()
    {
        GeneratorResult result = GeneratorTestHarness.Run(
            DiscoverySource,
            new Dictionary<string, string> { ["build_property.QuantixReportDiscovery"] = "true" });

        List<string> discovered = result.Diagnostics
            .Where(diagnostic => diagnostic.Id == "QTX0010")
            .Select(diagnostic => diagnostic.GetMessage())
            .ToList();

        Assert.Contains(discovered, message => message.Contains("GreetHandler"));
        Assert.Contains(discovered, message => message.Contains("LogBehavior"));
    }

    /// <summary>A command, its handler and an open-generic behavior — the QTX0010 fixture.</summary>
    private const string DiscoverySource = """
        using Quantix;
        using System.Threading;
        using System.Threading.Tasks;

        namespace App;

        public sealed record Greet(string Name) : ICommand<string>;

        public sealed class GreetHandler : ICommandHandler<Greet, string>
        {
            public ValueTask<string> Handle(Greet command, CancellationToken ct) => new("hi");
        }

        public sealed class LogBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
        {
            public ValueTask<TResult> Handle(
                TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct) => next(ct);
        }
        """;
}
