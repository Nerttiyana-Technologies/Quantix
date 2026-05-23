using Microsoft.Extensions.DependencyInjection;
using Quantix;
using Xunit;

namespace Quantix.IntegrationTests;

/// <summary>Integration tests for <see cref="IMediator"/>.Send through the generated dispatcher.</summary>
public class SendTests
{
    [Fact]
    public async Task Send_dispatches_a_command_to_its_handler()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddQuantix()
            .BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        int result = await mediator.Send(new Add(2, 3));

        Assert.Equal(5, result);
    }
}

/// <summary>A command that adds two numbers.</summary>
/// <param name="Left">The left operand.</param>
/// <param name="Right">The right operand.</param>
public sealed record Add(int Left, int Right) : ICommand<int>;

/// <summary>Handles <see cref="Add"/>.</summary>
public sealed class AddHandler : ICommandHandler<Add, int>
{
    /// <inheritdoc />
    public ValueTask<int> Handle(Add command, CancellationToken ct)
        => new(command.Left + command.Right);
}
