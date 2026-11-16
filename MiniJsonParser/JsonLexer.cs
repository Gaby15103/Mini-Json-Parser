using System.Text;

namespace MiniJsonParser;

/// <summary>
/// Performs lexical analysis on a raw JSON string, scanning characters sequentially
/// into a stream of <see cref="Token"/> instances consumed by <see cref="JsonParser"/>.
/// </summary>
public class JsonLexer
{
    /// <summary>
    /// The raw JSON source text being scanned.
    /// </summary>
    private readonly string _input;

    /// <summary>
    /// The zero-based current character index within <see cref="_input"/>.
    /// </summary>
    private int _pos;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonLexer"/> for the specified JSON input string.
    /// </summary>
    /// <param name="input">The raw JSON text to tokenize.</param>
    public JsonLexer(string input) => _input = input;

    /// <summary>
    /// Advances past any leading whitespace and scans the next <see cref="Token"/> from <see cref="_input"/>.
    /// </summary>
    /// <returns>
    /// The next <see cref="Token"/> in the input stream, or a token with <see cref="TokenType.Eof"/>
    /// when <see cref="_pos"/> reaches the end of <see cref="_input"/>.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when an unrecognized character is encountered at <see cref="_pos"/>.
    /// </exception>
    public Token NextToken()
    {
        while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
            _pos++;

        if (_pos >= _input.Length)
            return new Token(TokenType.Eof, "");

        char current = _input[_pos];
        switch (current)
        {
            case '{':
                _pos++;
                return new Token(TokenType.LeftBrace, "{");
            case '}':
                _pos++;
                return new Token(TokenType.RightBrace, "}");
            case '[':
                _pos++;
                return new Token(TokenType.LeftBracket, "[");
            case ']':
                _pos++;
                return new Token(TokenType.RightBracket, "]");
            case ':':
                _pos++;
                return new Token(TokenType.Colon, ":");
            case ',':
                _pos++;
                return new Token(TokenType.Comma, ",");
            case '"': return ReadString();
        }

        if (char.IsDigit(current) || current == '-')
            return ReadNumber();

        if (_input.AsSpan(_pos).StartsWith("true"))
        {
            _pos += 4;
            return new Token(TokenType.True, "true");
        }

        if (_input.AsSpan(_pos).StartsWith("false"))
        {
            _pos += 5;
            return new Token(TokenType.False, "false");
        }

        if (_input.AsSpan(_pos).StartsWith("null"))
        {
            _pos += 4;
            return new Token(TokenType.Null, "null");
        }

        throw new Exception($"Unexpected character '{current}' at position {_pos}");
    }

    /// <summary>
    /// Reads a double-quoted string literal starting at <see cref="_pos"/>,
    /// unescaping basic backslash sequences (<c>\"</c>, <c>\\</c>, <c>\n</c>, <c>\r</c>, <c>\t</c>).
    /// </summary>
    /// <returns>A <see cref="Token"/> with <see cref="TokenType.String"/> and the unescaped string content.</returns>
    private Token ReadString()
    {
        _pos++;
        var sb = new StringBuilder();
        while (_pos < _input.Length && _input[_pos] != '"')
        {
            if (_input[_pos] == '\\' && _pos + 1 < _input.Length)
            {
                _pos++;
                sb.Append(_input[_pos] switch
                {
                    '"' => '"', '\\' => '\\', 'n' => '\n', 'r' => '\r', 't' => '\t',
                    _ => _input[_pos]
                });
            }
            else
            {
                sb.Append(_input[_pos]);
            }

            _pos++;
        }

        _pos++;
        return new Token(TokenType.String, sb.ToString());
    }

    /// <summary>
    /// Reads an unquoted numeric literal (digits, sign, decimal point, and exponent characters) starting at <see cref="_pos"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="Token"/> with <see cref="TokenType.Number"/> containing the raw numeric slice,
    /// later wrapped in a <see cref="JsonNumber"/> by <see cref="JsonParser"/>.
    /// </returns>
    private Token ReadNumber()
    {
        int start = _pos;
        while (_pos < _input.Length && (char.IsDigit(_input[_pos]) || ".eE+-".Contains(_input[_pos])))
            _pos++;
        return new Token(TokenType.Number, _input[start.._pos]);
    }
}
