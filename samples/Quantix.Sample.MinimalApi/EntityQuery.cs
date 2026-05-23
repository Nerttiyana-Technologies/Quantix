using Quantix;

namespace Quantix.Sample;

/// <summary>An entity in the sample domain.</summary>
/// <param name="Id">The product identifier.</param>
public sealed record Product(int Id);

/// <summary>
/// A generic query — a message type that is itself generic (design D14). Quantix discovers the
/// open-generic handler below, finds every concrete instantiation constructed in the program
/// (here <c>DescribeEntity&lt;Product&gt;</c>, built by the endpoint), and emits one closed
/// dispatch path and closed registration per instantiation. No generic is closed at run time.
/// </summary>
/// <typeparam name="TEntity">The entity type being described.</typeparam>
/// <param name="Id">The identifier of the entity to describe.</param>
public sealed record DescribeEntity<TEntity>(int Id) : IQuery<string>;

/// <summary>Handles <see cref="DescribeEntity{TEntity}"/> for every entity type it is used with.</summary>
/// <typeparam name="TEntity">The entity type being described.</typeparam>
public sealed class DescribeEntityHandler<TEntity> : IQueryHandler<DescribeEntity<TEntity>, string>
{
    /// <inheritdoc />
    public ValueTask<string> Handle(DescribeEntity<TEntity> query, CancellationToken ct)
        => new($"{typeof(TEntity).Name} #{query.Id}");
}
