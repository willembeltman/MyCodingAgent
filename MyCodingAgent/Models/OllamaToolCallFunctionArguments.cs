namespace MyCodingAgent.Models;

public class OllamaToolCallFunctionArguments
{
    public string? id { get; set; }
    public string? action { get; set; }
    public string? path { get; set; }
    public string? newPath { get; set; }
    public string? query { get; set; }
    public string? content { get; set; }
    public string? replaceText { get; set; }
    public int? lineNumber { get; set; }
}
