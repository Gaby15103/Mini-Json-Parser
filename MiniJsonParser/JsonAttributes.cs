namespace MiniJsonParser;

/// <summary>
/// Specifies the exact JSON property name used when serializing or deserializing a property, field, or constructor parameter
/// with <see cref="JsonSerializer"/>, overriding the active <see cref="JsonSerializerOptions.NamingPolicy"/>.
/// </summary>
/// <remarks>
/// When applied to a primary constructor parameter on a <see langword="record"/> or <see langword="record struct"/>
/// using <c>[property: JsonPropertyName("...")]</c>, <see cref="JsonSerializer.Deserialize{T}(string, JsonSerializerOptions?)"/>
/// resolves the attribute from the matching property automatically.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class JsonPropertyNameAttribute : Attribute
{
    /// <summary>
    /// Gets the explicit JSON key name associated with the decorated member or parameter.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="JsonPropertyNameAttribute"/> with the specified JSON key name.
    /// </summary>
    /// <param name="name">The exact key name to emit in JSON output or match during deserialization.</param>
    public JsonPropertyNameAttribute(string name) => Name = name;
}

/// <summary>
/// Instructs <see cref="JsonSerializer.Serialize{T}(T, JsonSerializerOptions?)"/> and
/// <see cref="JsonSerializer.Deserialize{T}(string, JsonSerializerOptions?)"/> to skip the decorated property or field.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonIgnoreAttribute : Attribute
{
}