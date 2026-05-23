using Xunit;

namespace Quantix.Generator.Tests;

/// <summary>
/// Tests for constraint-aware scoping of open-generic pipeline behaviors (design section 4.4,
/// D11): the generator closes a constrained behavior over exactly the messages whose type
/// arguments satisfy its generic constraints — and over no others.
/// </summary>
public class ConstrainedBehaviorTests
{
    [Fact]
    public void A_command_constrained_behavior_wraps_commands_but_not_queries()
    {
        const string source = """
            using Quantix;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App;

            public sealed record DoThing(int Value) : ICommand<int>;
            public sealed class DoThingHandler : ICommandHandler<DoThing, int>
            {
                public ValueTask<int> Handle(DoThing command, CancellationToken ct) => new(command.Value);
            }

            public sealed record AskThing(int Value) : IQuery<int>;
            public sealed class AskThingHandler : IQueryHandler<AskThing, int>
            {
                public ValueTask<int> Handle(AskThing query, CancellationToken ct) => new(query.Value);
            }

            public sealed class CommandOnlyBehavior<TCommand, TResult> : IPipelineBehavior<TCommand, TResult>
                where TCommand : ICommand<TResult>
            {
                public ValueTask<TResult> Handle(
                    TCommand request, RequestHandlerDelegate<TResult> next, CancellationToken ct) => next(ct);
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.Empty(result.Diagnostics);
        Assert.Contains("CommandOnlyBehavior<global::App.DoThing, int>", result.AllGeneratedSource);
        Assert.DoesNotContain("CommandOnlyBehavior<global::App.AskThing", result.AllGeneratedSource);
    }

    [Fact]
    public void A_marker_constrained_behavior_wraps_only_the_marked_message()
    {
        const string source = """
            using Quantix;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App;

            public interface IAudited { }

            public sealed record Tracked(int Value) : ICommand<int>, IAudited;
            public sealed class TrackedHandler : ICommandHandler<Tracked, int>
            {
                public ValueTask<int> Handle(Tracked command, CancellationToken ct) => new(command.Value);
            }

            public sealed record Untracked(int Value) : ICommand<int>;
            public sealed class UntrackedHandler : ICommandHandler<Untracked, int>
            {
                public ValueTask<int> Handle(Untracked command, CancellationToken ct) => new(command.Value);
            }

            public sealed class AuditBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
                where TRequest : IAudited
            {
                public ValueTask<TResult> Handle(
                    TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct) => next(ct);
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.Empty(result.Diagnostics);
        Assert.Contains("AuditBehavior<global::App.Tracked, int>", result.AllGeneratedSource);
        Assert.DoesNotContain("AuditBehavior<global::App.Untracked", result.AllGeneratedSource);
    }

    [Fact]
    public void An_unconstrained_open_generic_behavior_still_wraps_every_message()
    {
        const string source = """
            using Quantix;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App;

            public sealed record DoThing(int Value) : ICommand<int>;
            public sealed class DoThingHandler : ICommandHandler<DoThing, int>
            {
                public ValueTask<int> Handle(DoThing command, CancellationToken ct) => new(command.Value);
            }

            public sealed record AskThing(int Value) : IQuery<int>;
            public sealed class AskThingHandler : IQueryHandler<AskThing, int>
            {
                public ValueTask<int> Handle(AskThing query, CancellationToken ct) => new(query.Value);
            }

            public sealed class LogBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
            {
                public ValueTask<TResult> Handle(
                    TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct) => next(ct);
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.Empty(result.Diagnostics);
        Assert.Contains("LogBehavior<global::App.DoThing, int>", result.AllGeneratedSource);
        Assert.Contains("LogBehavior<global::App.AskThing, int>", result.AllGeneratedSource);
    }
}
