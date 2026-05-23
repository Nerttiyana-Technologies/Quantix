using Quantix;

namespace Quantix.Sample;

/// <summary>
/// An open-generic pipeline behavior that logs every command and query flowing through the
/// mediator. Quantix discovers it, closes it over each message, and registers each closed
/// instantiation — no marker interface and no manual registration required.
/// </summary>
/// <typeparam name="TRequest">The request type being wrapped.</typeparam>
/// <typeparam name="TResult">The result type the request produces.</typeparam>
public sealed class LoggingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResult>> _logger;

    /// <summary>Creates the behavior with a logger resolved by dependency injection.</summary>
    /// <param name="logger">The logger.</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResult>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken ct)
    {
        _logger.LogInformation("Quantix: handling {Request}.", request);
        TResult result = await next(ct).ConfigureAwait(false);
        _logger.LogInformation("Quantix: handled {Request}, result {Result}.", request, result);
        return result;
    }
}
