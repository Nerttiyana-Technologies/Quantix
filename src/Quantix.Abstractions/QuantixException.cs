// Quantix.QuantixException — base type for the few runtime failure modes.

namespace Quantix;

/// <summary>
/// The base type for exceptions raised by Quantix at runtime.
/// </summary>
/// <remarks>
/// Quantix reports the overwhelming majority of problems — missing handlers, duplicate
/// handlers, signature mismatches — as build-time diagnostics. This exception type covers
/// the few conditions that can only surface at runtime.
/// </remarks>
public class QuantixException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="QuantixException"/> class.</summary>
    public QuantixException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuantixException"/> class with a
    /// message that describes the error.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public QuantixException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuantixException"/> class with a
    /// message and the exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public QuantixException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
