namespace MiniJsonParser;

/// <summary>
/// Performs recursive descent syntactic analysis on a stream of <see cref="Token"/> instances
/// produced by <see cref="JsonLexer"/>, converting raw JSON text into an intermediate object graph
/// consumed by <see cref="JsonSerializer"/>.
/// </summary>
public class JsonParser
{
    /// <summary>
    /// The underlying lexical scanner that supplies <see cref="Token"/> instances from the JSON input.
    /// </summary>
    private readonly JsonLexer _lexer;

    /// <summary>
    /// The lookahead <see cref="Token"/> currently being evaluated by the parser.
    /// </summary>
    private Token _current;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonParser"/> for the specified JSON string
    /// and primes <see cref="_current"/> with the first token from <see cref="JsonLexer.NextToken"/>.
    /// </summary>
    /// <param name="json">The raw JSON payload to parse.</param>
    public JsonParser(string json)
    {
        _lexer = new JsonLexer(json);
        _current = _lexer.NextToken();
    }

    /// <summary>
    /// Verifies that <see cref="_current"/> matches the <paramref name="expected"/> <see cref="TokenType"/>
    /// and advances to the next token via <see cref="JsonLexer.NextToken"/>.
    /// </summary>
    /// <param name="expected">The <see cref="TokenType"/> required by the JSON grammar at the current position.</param>
    /// <exception cref="Exception">
    /// Thrown when <see cref="_current"/> does not match <paramref name="expected"/>.
    /// </exception>
    private void Eat(TokenType expected)
    {
        if (_current.Type != expected)
            throw new Exception($"Expected {expected} but found {_current.Type}");
        _current = _lexer.NextToken();
    }

    /// <summary>
    /// Parses the root JSON value by delegating to <see cref="ParseValue"/>.
    /// </summary>
    /// <returns>
    /// An intermediate .NET representation of the parsed JSON: a <see cref="Dictionary{TKey, TValue}"/> for objects,
    /// a <see cref="List{T}"/> for arrays, a <see cref="JsonNumber"/> for numbers, or a primitive (<see cref="string"/>, <see cref="bool"/>, or <see langword="null"/>).
    /// </returns>
    public object? Parse() => ParseValue();

    /// <summary>
    /// Evaluates <see cref="_current"/> and dispatches to the corresponding grammar rule
    /// (<see cref="ParseObject"/>, <see cref="ParseArray"/>, or a scalar literal).
    /// </summary>
    /// <returns>The parsed .NET object, collection, <see cref="JsonNumber"/>, or scalar value.</returns>
    /// <exception cref="Exception">
    /// Thrown when <see cref="_current"/> has an unexpected <see cref="TokenType"/> that cannot start a JSON value.
    /// </exception>
    private object? ParseValue()
    {
        switch (_current.Type)
        {
            case TokenType.LeftBrace: return ParseObject();
            case TokenType.LeftBracket: return ParseArray();
            case TokenType.String:
                string str = _current.Value;
                Eat(TokenType.String);
                return str;
            case TokenType.Number:
                string rawNum = _current.Value;
                Eat(TokenType.Number);
                return new JsonNumber(rawNum);
            case TokenType.True: Eat(TokenType.True); return true;
            case TokenType.False: Eat(TokenType.False); return false;
            case TokenType.Null: Eat(TokenType.Null); return null;
            default: throw new Exception($"Unexpected token {_current.Type}");
        }
    }

    /// <summary>
    /// Parses a comma-separated sequence of key-value pairs enclosed in curly braces (<c>{...}</c>),
    /// recursively calling <see cref="ParseValue"/> for each property value.
    /// </summary>
    /// <returns>A <see cref="Dictionary{TKey, TValue}"/> mapping JSON property names to their parsed values.</returns>
    private Dictionary<string, object?> ParseObject()
    {
        var obj = new Dictionary<string, object?>();
        Eat(TokenType.LeftBrace);
        if (_current.Type != TokenType.RightBrace)
        {
            while (true)
            {
                string key = _current.Value;
                Eat(TokenType.String);
                Eat(TokenType.Colon);
                obj[key] = ParseValue();

                if (_current.Type == TokenType.Comma) Eat(TokenType.Comma);
                else break;
            }
        }
        Eat(TokenType.RightBrace);
        return obj;
    }

    /// <summary>
    /// Parses a comma-separated sequence of elements enclosed in square brackets (<c>[...]</c>),
    /// recursively calling <see cref="ParseValue"/> for each element.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> containing the parsed elements in order.</returns>
    private List<object?> ParseArray()
    {
        var list = new List<object?>();
        Eat(TokenType.LeftBracket);
        if (_current.Type != TokenType.RightBracket)
        {
            while (true)
            {
                list.Add(ParseValue());
                if (_current.Type == TokenType.Comma) Eat(TokenType.Comma);
                else break;
            }
        }

        Eat(TokenType.RightBracket);
        return list;
    }
}
