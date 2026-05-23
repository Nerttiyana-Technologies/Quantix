using Microsoft.Extensions.DependencyInjection;
using Quantix;
using Xunit;

namespace Quantix.IntegrationTests;

/// <summary>
/// Integration tests for generic messages (design D14): an open-generic handler is closed over
/// each concrete instantiation and dispatched through the generated, reflection-free pipeline.
/// </summary>
public class GenericMessageTests
{
    [Fact]
    public async Task A_generic_message_dispatches_through_its_closed_open_generic_handler()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddQuantix()
            .BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        // Constructing IdentifyEntity<Widget> here is the instantiation the generator discovers,
        // closes IdentifyEntityHandler<Widget> over, and emits a dispatch path for.
        string result = await mediator.Send(new IdentifyEntity<Widget>(7));

        Assert.Equal("Widget #7", result);
    }
}

/// <summary>An entity type used to instantiate the generic message.</summary>
public sealed record Widget(int Id);

/// <summary>A generic query — a message type that is itself generic.</summary>
/// <typeparam name="TEntity">The entity type being identified.</typeparam>
/// <param name="Id">The identifier of the entity.</param>
public sealed record IdentifyEntity<TEntity>(int Id) : IQuery<string>;

/// <summary>Handles <see cref="IdentifyEntity{TEntity}"/> for every entity type it is used with.</summary>
/// <typeparam name="TEntity">The entity type being identified.</typeparam>
public sealed class IdentifyEntityHandler<TEntity> : IQueryHandler<IdentifyEntity<TEntity>, string>
{
    /// <inheritdoc />
    public ValueTask<string> Handle(IdentifyEntity<TEntity> query, CancellationToken ct)
        => new($"{typeof(TEntity).Name} #{query.Id}");
}
