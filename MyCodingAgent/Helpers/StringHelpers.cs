using MyCodingAgent.Models;

namespace MyCodingAgent.Helpers;

public static class StringHelpers
{
    public static Line[] GetLines(this string value)
    {
        var lines = value.Split('\n');
        return [.. Enumerable.Range(1, lines.Length).Select(i => new Line(i, lines[i - 1].Trim('\r')))];
    }
    public static int GetLineCount(this string value) => GetLines(value).Length;

    public static string? JsonEscape(this string? value)
    {
        if (value == null) return null;
        var sb = new System.Text.StringBuilder();
        foreach (var c in value)
        {
            switch (c)
            {
                case '\"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (char.IsControl(c))
                        sb.Append("\\u" + ((int)c).ToString("x4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}