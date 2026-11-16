namespace MiniJsonParser;

/// <summary>
/// Specifies how C# property and field names are formatted when serialized into JSON keys
/// by <see cref="JsonSerializer.Serialize{T}(T, JsonSerializerOptions?)"/>.
/// </summary>
public enum JsonNamingPolicy
{
    /// <summary>
    /// Preserves the exact C# member name without casing changes (for example, <c>AccessToken</c> remains <c>AccessToken</c>).
    /// </summary>
    AsIs,

    /// <summary>
    /// Converts the first character of the C# member name to lowercase (for example, <c>AccessToken</c> becomes <c>accessToken</c>).
    /// </summary>
    CamelCase,

    /// <summary>
    /// Separates uppercase words with underscores and converts all characters to lowercase (for example, <c>AccessToken</c> becomes <c>access_token</c>).
    /// </summary>
    SnakeCase
}

/// <summary>
/// Provides configuration settings that control key naming and matching behavior for
/// <see cref="JsonSerializer.Serialize{T}(T, JsonSerializerOptions?)"/> and
/// <see cref="JsonSerializer.Deserialize{T}(string, JsonSerializerOptions?)"/>.
/// </summary>
public sealed class JsonSerializerOptions
{
    /// <summary>
    /// Gets the default shared instance of <see cref="JsonSerializerOptions"/>, configured with
    /// <see cref="JsonNamingPolicy.CamelCase"/> and <see cref="FlexibleKeyMatching"/> enabled.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new();

    /// <summary>
    /// Gets or sets the naming convention used by <see cref="JsonSerializer.Serialize{T}(T, JsonSerializerOptions?)"/>
    /// when emitting JSON property names that do not have an explicit <see cref="JsonPropertyNameAttribute"/>.
    /// Defaults to <see cref="JsonNamingPolicy.CamelCase"/>.
    /// </summary>
    public JsonNamingPolicy NamingPolicy { get; set; } = JsonNamingPolicy.CamelCase;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="JsonSerializer.Deserialize{T}(string, JsonSerializerOptions?)"/>
    /// normalizes separators (<c>_</c>, <c>-</c>) and casing when matching incoming JSON keys to C# properties, fields, or constructor parameters.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool FlexibleKeyMatching { get; set; } = true;
}
