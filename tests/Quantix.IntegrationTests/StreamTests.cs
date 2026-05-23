using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Quantix;
using Xunit;

namespace Quantix.IntegrationTests;

/// <summary>Integration tests for <see cref="IMediator"/>.Stream through the generated dispatcher.</summary>
public class StreamTests
{
    [Fact]
    public async Task Stream_yields_the_handler_sequence()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddQuantix()
            .BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        var received = new List<int>();
        await foreach (int value in mediator.Stream(new CountTo(3)))
        {
            received.Add(value);
        }

        Assert.Equal(new[] { 1, 2, 3 }, received);
    }
}

/// <summary>A stream request that yields the integers from 1 to <see cref="Max"/>.</summary>
/// <param name="Max">The largest integer to yield.</param>
public sealed record CountTo(int Max) : IStreamRequest<int>;

/// <summary>Handles <see cref="CountTo"/>.</summary>
public sealed class CountToHandler : IStreamRequestHandler<CountTo, int>
{
    /// <inheritdoc />
    public async IAsyncEnumerable<int> Handle(CountTo request, [EnumeratorCancellation] CancellationToken ct)
    {
        for (int i = 1; i <= request.Max; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}
