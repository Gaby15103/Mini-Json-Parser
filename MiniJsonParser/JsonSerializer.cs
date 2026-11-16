using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace MiniJsonParser;

/// <summary>
/// Provides reflection-based JSON serialization and deserialization for .NET classes, structs, and records,
/// bridging raw JSON payloads parsed by <see cref="JsonParser"/> with strongly typed .NET models.
/// </summary>
public static class JsonSerializer
{
    /// <summary>
    /// Deserializes a JSON object string into an instance of <typeparamref name="T"/> using <see cref="JsonParser"/>
    /// and runtime reflection via <see cref="MapObject"/>.
    /// </summary>
    /// <typeparam name="T">The target .NET <see langword="class"/>, <see langword="struct"/>, or <see langword="record"/> type.</typeparam>
    /// <param name="json">The raw JSON string to deserialize. The root value must be a JSON object (<c>{...}</c>).</param>
    /// <param name="options">
    /// Optional <see cref="JsonSerializerOptions"/> controlling key matching behavior.
    /// Uses <see cref="JsonSerializerOptions.Default"/> if <see langword="null"/>.
    /// </param>
    /// <returns>A populated instance of <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the root JSON token parsed by <see cref="JsonParser.Parse"/> is not a JSON object,
    /// or when <typeparamref name="T"/> cannot be instantiated.
    /// </exception>
    public static T Deserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        options ??= JsonSerializerOptions.Default;
        object? root = new JsonParser(json).Parse();

        if (root is not Dictionary<string, object?> rootObject)
            throw new InvalidOperationException("Root JSON payload must be a JSON object");

        return (T)MapObject(rootObject, typeof(T), options);
    }

    /// <summary>
    /// Serializes a .NET object, struct, or record of type <typeparamref name="T"/> into a JSON string
    /// by inspecting public properties and fields via reflection.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The object or struct instance to convert into JSON.</param>
    /// <param name="options">
    /// Optional <see cref="JsonSerializerOptions"/> controlling <see cref="JsonNamingPolicy"/> formatting.
    /// Uses <see cref="JsonSerializerOptions.Default"/> if <see langword="null"/>.
    /// </param>
    /// <returns>A JSON-formatted string representing <paramref name="value"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a circular object reference is detected during recursive traversal in <see cref="SerializeValue"/>.
    /// </exception>
    public static string Serialize<T>(T value, JsonSerializerOptions? options = null)
    {
        options ??= JsonSerializerOptions.Default;
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return SerializeValue(value, options, visited);
    }

    /// <summary>
    /// Recursively converts an arbitrary .NET value (primitive, enum, collection, class, or struct) into its JSON text representation.
    /// </summary>
    /// <remarks>
    /// Separated from <see cref="Serialize{T}"/> so nested objects and collections can be traversed recursively
    /// while sharing the active ancestor <paramref name="visited"/> set.
    /// </remarks>
    /// <param name="obj">The current .NET object or scalar value being serialized.</param>
    /// <param name="options">The active <see cref="JsonSerializerOptions"/> governing key casing via <see cref="GetOutgoingKey"/>.</param>
    /// <param name="visited">
    /// A reference-equality <see cref="HashSet{T}"/> tracking ancestor reference types currently on the call stack
    /// to detect and prevent infinite recursion from circular references (such as <c>A -&gt; B -&gt; A</c>).
    /// </param>
    /// <returns>The JSON fragment representing <paramref name="obj"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="obj"/> is a reference type already present in <paramref name="visited"/>.
    /// </exception>
    private static string SerializeValue(object? obj, JsonSerializerOptions options, HashSet<object> visited)
    {
        if (obj is null) return "null";

        Type type = obj.GetType();

        if (obj is string s) return $"\"{Escape(s)}\"";
        if (obj is bool b) return b ? "true" : "false";
        if (type.IsEnum) return $"\"{obj}\"";
        if (type.IsPrimitive || obj is decimal)
            return Convert.ToString(obj, CultureInfo.InvariantCulture) ?? "0";

        if (!type.IsValueType && !visited.Add(obj))
            throw new InvalidOperationException($"Circular reference detected for type {type.Name}.");

        if (obj is IEnumerable enumerable)
        {
            var items = new List<string>();
            foreach (object? item in enumerable)
                items.Add(SerializeValue(item, options, visited));

            if (!type.IsValueType) visited.Remove(obj);
            return $"[{string.Join(",", items)}]";
        }

        var members = new List<string>();

        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetCustomAttribute(typeof(JsonIgnoreAttribute)) != null)
                continue;

            string key = GetOutgoingKey(prop, options);
            members.Add($"\"{Escape(key)}\":{SerializeValue(prop.GetValue(obj), options, visited)}");
        }

        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.GetCustomAttribute(typeof(JsonIgnoreAttribute)) != null)
                continue;

            string key = GetOutgoingKey(field, options);
            members.Add($"\"{Escape(key)}\":{SerializeValue(field.GetValue(obj), options, visited)}");
        }

        if (!type.IsValueType) visited.Remove(obj);

        return $"{{{string.Join(",", members)}}}";
    }

    /// <summary>
    /// Converts an intermediate parsed value from <see cref="JsonParser"/> into the specified <paramref name="targetType"/>.
    /// </summary>
    /// <param name="raw">
    /// The intermediate value produced by <see cref="JsonParser"/> (such as a <see cref="JsonNumber"/>,
    /// <see cref="Dictionary{TKey, TValue}"/>, <see cref="List{T}"/>, or primitive).
    /// </param>
    /// <param name="targetType">The destination .NET <see cref="Type"/> (including <see cref="Nullable{T}"/> wrappers).</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> passed down to <see cref="MapObject"/> and <see cref="MapCollection"/>.</param>
    /// <returns>A value converted to <paramref name="targetType"/>, or <see langword="null"/> if <paramref name="raw"/> is <see langword="null"/>.</returns>
    private static object? MapValue(object? raw, Type targetType, JsonSerializerOptions options)
    {
        if (raw is null) return null;

        Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (raw is JsonNumber num)
        {
            if (underlying.IsEnum)
            {
                object underlyingVal = Convert.ChangeType(
                    long.Parse(num.Raw, CultureInfo.InvariantCulture),
                    Enum.GetUnderlyingType(underlying));
                return Enum.ToObject(underlying, underlyingVal);
            }

            if (underlying == typeof(int)) return int.Parse(num.Raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(long)) return long.Parse(num.Raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(decimal)) return decimal.Parse(num.Raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(double)) return double.Parse(num.Raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(float)) return float.Parse(num.Raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(short)) return short.Parse(num.Raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(byte)) return byte.Parse(num.Raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(uint)) return uint.Parse(num.Raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(ulong)) return ulong.Parse(num.Raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(string)) return num.Raw;

            return long.TryParse(num.Raw, CultureInfo.InvariantCulture, out long l)
                ? l
                : double.Parse(num.Raw, CultureInfo.InvariantCulture);
        }

        if (underlying == typeof(string)) return raw.ToString();
        if (underlying.IsEnum && raw is string e) return Enum.Parse(underlying, e, ignoreCase: true);
        if (underlying.IsPrimitive || underlying == typeof(decimal))
            return Convert.ChangeType(raw, underlying, CultureInfo.InvariantCulture);

        if (raw is List<object?> list)
            return MapCollection(list, underlying, options);

        if (raw is Dictionary<string, object?> dict)
            return MapObject(dict, underlying, options);

        return raw;
    }

    /// <summary>
    /// Instantiates and populates a .NET <see langword="class"/>, <see langword="struct"/>, or <see langword="record"/>
    /// from a parsed JSON key-value <paramref name="dict"/> using reflection.
    /// </summary>
    /// <remarks>
    /// Resolves parameterized constructors first for immutable records and primary-constructor structs,
    /// falling back to <see cref="Activator.CreateInstance(Type)"/> with <see cref="PropertyInfo.SetValue(object?, object?)"/>
    /// and <see cref="FieldInfo.SetValue(object?, object?)"/> (keeping value types boxed during mutation).
    /// </remarks>
    /// <param name="dict">The key-value pairs representing a JSON object parsed by <see cref="JsonParser"/>.</param>
    /// <param name="targetType">The concrete .NET <see cref="Type"/> to instantiate and hydrate.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> used by <see cref="FindKey"/> to match JSON keys.</param>
    /// <returns>A populated instance of <paramref name="targetType"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Activator.CreateInstance(Type)"/> fails to construct <paramref name="targetType"/>.
    /// </exception>
    private static object MapObject(Dictionary<string, object?> dict, Type targetType, JsonSerializerOptions options)
    {
        ConstructorInfo? paramCtor = targetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault(c => c.GetParameters().Length > 0);

        bool hasParameterlessCtor = targetType.GetConstructor(Type.EmptyTypes) != null;

        if (paramCtor != null && (!hasParameterlessCtor || targetType.IsValueType))
        {
            ParameterInfo[] parameters = paramCtor.GetParameters();
            PropertyInfo[] properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            object?[] args = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo param = parameters[i];
                PropertyInfo? matchingProp = properties.FirstOrDefault(p =>
                    string.Equals(p.Name, param.Name, StringComparison.OrdinalIgnoreCase));

                var paramAttr = (JsonPropertyNameAttribute?)param.GetCustomAttribute(typeof(JsonPropertyNameAttribute));
                var propAttr =
                    (JsonPropertyNameAttribute?)matchingProp?.GetCustomAttribute(typeof(JsonPropertyNameAttribute));

                string keyName = paramAttr?.Name ?? propAttr?.Name ?? param.Name!;
                var match = FindKey(dict, keyName, options);

                args[i] = match.Key != null
                    ? MapValue(match.Value, param.ParameterType, options)
                    : (param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null);
            }

            return paramCtor.Invoke(args);
        }


        object boxedInstance = Activator.CreateInstance(targetType)
                               ?? throw new InvalidOperationException($"Cannot creat e instance of {targetType.Name}");

        foreach (PropertyInfo prop in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite || prop.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                continue;

            string keyName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name;
            var match = FindKey(dict, keyName, options);

            if (match.Key != null)
                prop.SetValue(boxedInstance, MapValue(match.Value, prop.PropertyType, options));
        }

        foreach (FieldInfo field in targetType.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                continue;

            string keyName = field.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? field.Name;
            var match = FindKey(dict, keyName, options);

            if (match.Key != null)
                field.SetValue(boxedInstance, MapValue(match.Value, field.FieldType, options));
        }

        return boxedInstance;
    }

    /// <summary>
    /// Converts a parsed JSON array (<paramref name="rawList"/>) into a strongly typed .NET <see cref="Array"/>
    /// or generic <see cref="List{T}"/> matching <paramref name="targetType"/>.
    /// </summary>
    /// <param name="rawList">The list of intermediate elements parsed by <see cref="JsonParser"/>.</param>
    /// <param name="targetType">The destination array or generic collection <see cref="Type"/>.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> forwarded to <see cref="MapValue"/> for each element.</param>
    /// <returns>A populated <see cref="Array"/> or <see cref="IList"/> instance.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="targetType"/> is neither an array nor a generic collection type.
    /// </exception>
    private static object MapCollection(List<object?> rawList, Type targetType, JsonSerializerOptions options)
    {
        if (targetType.IsArray)
        {
            Type elemType = targetType.GetElementType()!;
            Array arr = Array.CreateInstance(elemType, rawList.Count);
            for (int i = 0; i < rawList.Count; i++)
                arr.SetValue(MapValue(rawList[i], elemType, options), i);
            return arr;
        }

        if (targetType.IsGenericType)
        {
            Type itemType = targetType.GetGenericArguments()[0];
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;
            foreach (object? item in rawList)
                list.Add(MapValue(item, itemType, options));
            return list;
        }

        throw new NotSupportedException($"Le type de collection {targetType.Name} n'est pas supporté.");
    }

    /// <summary>
    /// Determines the outgoing JSON property key for a <see cref="MemberInfo"/>, prioritizing
    /// <see cref="JsonPropertyNameAttribute"/> over <see cref="JsonSerializerOptions.NamingPolicy"/>.
    /// </summary>
    /// <param name="member">The <see cref="PropertyInfo"/> or <see cref="FieldInfo"/> being serialized.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> specifying the active <see cref="JsonNamingPolicy"/>.</param>
    /// <returns>The formatted JSON key name.</returns>
    private static string GetOutgoingKey(MemberInfo member, JsonSerializerOptions options)
    {
        var attr = (JsonPropertyNameAttribute?)member.GetCustomAttribute(typeof(JsonPropertyNameAttribute));
        if (attr != null) return attr.Name;

        return options.NamingPolicy switch
        {
            JsonNamingPolicy.CamelCase => ToCamelCase(member.Name),
            JsonNamingPolicy.SnakeCase => ToSnakeCase(member.Name),
            _ => member.Name
        };
    }

    /// <summary>
    /// Searches <paramref name="dict"/> for an entry matching <paramref name="targetName"/>,
    /// falling back to <see cref="NormalizeKey"/> comparison when <see cref="JsonSerializerOptions.FlexibleKeyMatching"/> is enabled.
    /// </summary>
    /// <param name="dict">The parsed JSON object dictionary produced by <see cref="JsonParser"/>.</param>
    /// <param name="targetName">The C# member name or <see cref="JsonPropertyNameAttribute.Name"/> to look up.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> controlling <see cref="JsonSerializerOptions.FlexibleKeyMatching"/>.</param>
    /// <returns>
    /// The matching <see cref="KeyValuePair{TKey, TValue}"/>, or a default pair with a <see langword="null"/> key if not found.
    /// </returns>
    private static KeyValuePair<string, object?> FindKey(
        Dictionary<string, object?> dict,
        string targetName,
        JsonSerializerOptions options)
    {
        var exact = dict.FirstOrDefault(kv =>
            string.Equals(kv.Key, targetName, StringComparison.OrdinalIgnoreCase));

        if (exact.Key != null || !options.FlexibleKeyMatching)
            return exact;

        string normalizedTarget = NormalizeKey(targetName);
        return dict.FirstOrDefault(kv => NormalizeKey(kv.Key) == normalizedTarget);
    }

    /// <summary>
    /// Strips underscores (<c>_</c>) and hyphens (<c>-</c>) and converts <paramref name="key"/> to lowercase
    /// so <c>snake_case</c>, <c>kebab-case</c>, <c>camelCase</c>, and <c>PascalCase</c> keys normalize to the same canonical form.
    /// </summary>
    /// <param name="key">The property or JSON key name to normalize.</param>
    /// <returns>A canonical lowercase string without word separators.</returns>
    private static string NormalizeKey(string key)
        => key.Replace("_", "").Replace("-", "").ToLowerInvariant();

    /// <summary>
    /// Converts a <c>PascalCase</c> identifier into <c>camelCase</c> for <see cref="JsonNamingPolicy.CamelCase"/>.
    /// </summary>
    /// <param name="s">The input identifier.</param>
    /// <returns>The <c>camelCase</c> string.</returns>
    private static string ToCamelCase(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s.First()) + s.Substring(1);

    /// <summary>
    /// Converts a <c>PascalCase</c> or <c>camelCase</c> identifier into <c>snake_case</c> for <see cref="JsonNamingPolicy.SnakeCase"/>.
    /// </summary>
    /// <param name="s">The input identifier.</param>
    /// <returns>The <c>snake_case</c> string.</returns>
    private static string ToSnakeCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        var sb = new StringBuilder(s.Length + 4);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes backslashes (<c>\</c>) and double quotes (<c>"</c>) in <paramref name="s"/>
    /// so it can be safely embedded inside a JSON string literal by <see cref="SerializeValue"/>.
    /// </summary>
    /// <param name="s">The raw unescaped string value.</param>
    /// <returns>A JSON-safe string with backslashes and quotes escaped.</returns>
    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}