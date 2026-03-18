using MyCodingAgent.Models;

namespace MyCodingAgent.Helpers;

public static class StringHelpers
{
    public static Line[] GetLines(this string value)
    {
        var lines = value.Split('\n');
        return Enumerable.Range(1, lines.Length)
            .Select(i => new Line(i, lines[i - 1].Trim('\r')))
            .ToArray();
    }
    public static int GetLineCount(this string value) => GetLines(value).Length;
}