using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace SharpInspect.Server.Json;

/// <summary>
///     기본 타입(string, number, bool, null, 배열, 딕셔너리)을 처리하는
///     .NET Framework 3.5+ 호환 간단한 JSON 직렬화/역직렬화기.
/// </summary>
public static class SimpleJson
{
    /// <summary>
    ///     객체를 JSON 문자열로 직렬화합니다.
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

            // 인덱서 건너뛰기
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

            // 속성 이름을 camelCase로 변환
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

        if (value is string s)
        {
            SerializeString(s, sb);
        }
        else if (value is bool b)
        {
            sb.Append(b ? "true" : "false");
        }
        else if (value is int or long or short or byte or uint or ulong or ushort or sbyte)
        {
            sb.Append(value.ToString());
        }
        else if (value is float f)
        {
            sb.Append(f.ToString(CultureInfo.InvariantCulture));
        }
        else if (value is double d)
        {
            sb.Append(d.ToString(CultureInfo.InvariantCulture));
        }
        else if (value is decimal value1)
        {
            sb.Append(value1.ToString(CultureInfo.InvariantCulture));
        }
        else if (value is DateTime time)
        {
            sb.Append('"');
            sb.Append(time.ToString("o"));
            sb.Append('"');
        }
        else if (value is Enum)
        {
            sb.Append('"');
            sb.Append(value.ToString());
            sb.Append('"');
        }
        else if (value is IDictionary dictionary)
        {
            SerializeDictionary(dictionary, sb);
        }
        else if (value is IEnumerable enumerable)
        {
            SerializeArray(enumerable, sb);
        }
        else
        {
            SerializeObject(value, sb);
        }
    }
}
