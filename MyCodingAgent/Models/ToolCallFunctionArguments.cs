namespace MyCodingAgent.Models;

public class ToolCallFunctionArguments
{
    public string? Id { get; set; }
    public string? Action { get; set; }
    public string? Path { get; set; }
    public string? NewPath { get; set; }
    public string? Query { get; set; }
    public string? Content { get; set; }
    public string? ReplaceText { get; set; }
    public int? LineNumber { get; set; }
}
