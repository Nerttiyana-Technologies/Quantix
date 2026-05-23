// A small indentation-aware writer used to emit generated C# source (plan L2-D).

using System.Text;

namespace Quantix.Generator;

/// <summary>
/// A minimal indentation-aware text writer for emitting generated C# source. Lines are written
/// at the current indentation; <see cref="OpenBrace"/> and <see cref="CloseBrace"/> manage both
/// the brace and the indentation level.
/// </summary>
internal sealed class CodeWriter
{
    private const string IndentUnit = "    ";

    private readonly StringBuilder _builder = new();
    private int _indentLevel;

    /// <summary>Writes an empty line.</summary>
    /// <returns>This writer, for chaining.</returns>
    public CodeWriter Line()
    {
        _builder.Append('\n');
        return this;
    }

    /// <summary>Writes a line of text at the current indentation.</summary>
    /// <param name="text">The line to write; an empty string writes a blank line.</param>
    /// <returns>This writer, for chaining.</returns>
    public CodeWriter Line(string text)
    {
        if (text.Length != 0)
        {
            for (int i = 0; i < _indentLevel; i++)
            {
                _builder.Append(IndentUnit);
            }

            _builder.Append(text);
        }

        _builder.Append('\n');
        return this;
    }

    /// <summary>Writes an opening brace, then increases the indentation level.</summary>
    /// <returns>This writer, for chaining.</returns>
    public CodeWriter OpenBrace()
    {
        Line("{");
        _indentLevel++;
        return this;
    }

    /// <summary>Decreases the indentation level, then writes a closing brace.</summary>
    /// <returns>This writer, for chaining.</returns>
    public CodeWriter CloseBrace()
    {
        if (_indentLevel > 0)
        {
            _indentLevel--;
        }

        Line("}");
        return this;
    }

    /// <summary>Returns the accumulated source text.</summary>
    /// <returns>The generated source.</returns>
    public override string ToString() => _builder.ToString();
}
