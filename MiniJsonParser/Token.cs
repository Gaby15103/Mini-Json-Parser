namespace MiniJsonParser;

/// <summary>
/// Represents a single lexical unit scanned by <see cref="JsonLexer.NextToken"/> 
/// and consumed by <see cref="JsonParser"/> during syntax analysis.
/// </summary>
/// <param name="Type">The grammatical category of the token, defined by <see cref="TokenType"/>.</param>
/// <param name="Value">The raw text value extracted from the JSON input.</param>
public record Token(TokenType Type, string Value);

/// <summary>
/// Defines the valid lexical token categories recognized by <see cref="JsonLexer"/> 
/// and evaluated by <see cref="JsonParser"/>.
/// </summary>
public enum TokenType
{
    /// <summary>Opening curly brace (<c>{</c>) indicating the start of a JSON object.</summary>
    LeftBrace,

    /// <summary>Closing curly brace (<c>}</c>) indicating the end of a JSON object.</summary>
    RightBrace,

    /// <summary>Opening square bracket (<c>[</c>) indicating the start of a JSON array.</summary>
    LeftBracket,

    /// <summary>Closing square bracket (<c>]</c>) indicating the end of a JSON array.</summary>
    RightBracket,

    /// <summary>Colon (<c>:</c>) separating a property name from its value inside a JSON object.</summary>
    Colon,

    /// <summary>Comma (<c>,</c>) separating elements in an array or key-value pairs in an object.</summary>
    Comma,

    /// <summary>A double-quoted JSON string literal with escape sequences resolved.</summary>
    String,

    /// <summary>An unquoted numeric literal (integer, decimal, or scientific notation).</summary>
    Number,

    /// <summary>The JSON boolean literal <c>true</c>.</summary>
    True,

    /// <summary>The JSON boolean literal <c>false</c>.</summary>
    False,

    /// <summary>The JSON <c>null</c> literal.</summary>
    Null,

    /// <summary>End-of-file marker indicating that <see cref="JsonLexer"/> has reached the end of the input string.</summary>
    Eof
}

/// <summary>
/// Preserves the exact raw string representation of a JSON numeric literal parsed by <see cref="JsonParser"/>
/// so <see cref="JsonSerializer"/> can convert it to the target .NET numeric type (<see cref="int"/>, <see cref="long"/>, <see cref="decimal"/>, etc.) without floating-point precision loss.
/// </summary>
/// <param name="Raw">The unparsed numeric text captured from a <see cref="TokenType.Number"/> token.</param>
public readonly record struct JsonNumber(string Raw);