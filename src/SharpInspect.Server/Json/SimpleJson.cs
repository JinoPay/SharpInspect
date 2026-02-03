using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace SharpInspect.Server.Json;

/// <summary>
///     Simple JSON serializer/deserializer compatible with .NET Framework 3.5+.
///     Handles basic types: string, number, bool, null, arrays, and dictionaries.
/// </summary>
public static class SimpleJson
{
    /// <summary>
    ///     Serializes an object to JSON string.
    /// </summary>
    public static string Serialize(object obj)
    {
        if (obj == null)
            return "null";

        var sb = new StringBuilder();
        SerializeValue(obj, sb);
        return sb.ToString();
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        if (name.Length == 1)
            return name.ToLowerInvariant();

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static void SerializeArray(IEnumerable array, StringBuilder sb)
    {
        sb.Append('[');
        var first = true;
        foreach (var item in array)
        {
            if (!first)
                sb.Append(',');
            first = false;
            SerializeValue(item, sb);
        }

        sb.Append(']');
    }

    private static void SerializeDictionary(IDictionary dict, StringBuilder sb)
    {
        sb.Append('{');
        var first = true;
        foreach (DictionaryEntry entry in dict)
        {
            if (!first)
                sb.Append(',');
            first = false;

            SerializeString(entry.Key.ToString(), sb);
            sb.Append(':');
            SerializeValue(entry.Value, sb);
        }

        sb.Append('}');
    }

    private static void SerializeObject(object obj, StringBuilder sb)
    {
        var type = obj.GetType();
        var properties = type.GetProperties();

        sb.Append('{');
        var first = true;

        foreach (var prop in properties)
        {
            if (!prop.CanRead)
                continue;

            // Skip indexers
            if (prop.GetIndexParameters().Length > 0)
                continue;

            object value;
            try
            {
                value = prop.GetValue(obj, null);
            }
            catch
            {
                continue;
            }

            if (!first)
                sb.Append(',');
            first = false;

            // Convert property name to camelCase
            var name = ToCamelCase(prop.Name);
            SerializeString(name, sb);
            sb.Append(':');
            SerializeValue(value, sb);
        }

        sb.Append('}');
    }

    private static void SerializeString(string str, StringBuilder sb)
    {
        sb.Append('"');
        foreach (var c in str)
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (c < 32)
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    break;
            }

        sb.Append('"');
    }

    private static void SerializeValue(object value, StringBuilder sb)
    {
        if (value == null)
        {
            sb.Append("null");
            return;
        }

        var type = value.GetType();

        if (value is string)
        {
            SerializeString((string)value, sb);
        }
        else if (value is bool)
        {
            sb.Append((bool)value ? "true" : "false");
        }
        else if (value is int || value is long || value is short || value is byte ||
                 value is uint || value is ulong || value is ushort || value is sbyte)
        {
            sb.Append(value.ToString());
        }
        else if (value is float)
        {
            sb.Append(((float)value).ToString(CultureInfo.InvariantCulture));
        }
        else if (value is double)
        {
            sb.Append(((double)value).ToString(CultureInfo.InvariantCulture));
        }
        else if (value is decimal)
        {
            sb.Append(((decimal)value).ToString(CultureInfo.InvariantCulture));
        }
        else if (value is DateTime)
        {
            sb.Append('"');
            sb.Append(((DateTime)value).ToString("o"));
            sb.Append('"');
        }
        else if (value is Enum)
        {
            sb.Append('"');
            sb.Append(value.ToString());
            sb.Append('"');
        }
        else if (value is IDictionary)
        {
            SerializeDictionary((IDictionary)value, sb);
        }
        else if (value is IEnumerable)
        {
            SerializeArray((IEnumerable)value, sb);
        }
        else
        {
            SerializeObject(value, sb);
        }
    }
}