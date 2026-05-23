using Microsoft.CodeAnalysis;
using Xunit;

namespace Quantix.Generator.Tests;

/// <summary>
/// Tests for the navigation analyzer (design section 6.1, D12): the QTX0012 handler hint names
/// the handler that runs at a Send, Publish or Stream call site.
/// </summary>
public class NavigationAnalyzerTests
{
    [Fact]
    public void Reports_the_handler_at_a_Send_call()
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

            public static class Caller
            {
                public static ValueTask<string> Run(IMediator mediator) => mediator.Send(new Greet("x"));
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = AnalyzerTestHarness.Run(source);

        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Id == "QTX0012"
                && diagnostic.GetMessage().Contains("Greet")
                && diagnostic.GetMessage().Contains("GreetHandler"));
    }

    [Fact]
    public void Reports_every_handler_at_a_Publish_call()
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

            public static class Caller
            {
                public static ValueTask Run(IMediator mediator) => mediator.Publish(new Pinged());
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = AnalyzerTestHarness.Run(source);

        string hint = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "QTX0012").GetMessage();
        Assert.Contains("FirstHandler", hint);
        Assert.Contains("SecondHandler", hint);
    }

    [Fact]
    public void Does_not_report_when_there_is_no_mediator_call()
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

        IReadOnlyList<Diagnostic> diagnostics = AnalyzerTestHarness.Run(source);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "QTX0012");
    }
}
