using Quantix;

namespace Quantix.Sample;

/// <summary>A query that asks for a personalised greeting.</summary>
/// <param name="Name">The name to greet.</param>
public sealed record GetGreeting(string Name) : IQuery<string>;

/// <summary>Handles <see cref="GetGreeting"/> — the handler Quantix discovers and wires up.</summary>
public sealed class GetGreetingHandler : IQueryHandler<GetGreeting, string>
{
    /// <inheritdoc />
    public ValueTask<string> Handle(GetGreeting query, CancellationToken ct)
        => new($"Hello, {query.Name}!");
}
