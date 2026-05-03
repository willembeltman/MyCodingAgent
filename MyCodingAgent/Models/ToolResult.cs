namespace MyCodingAgent.Models;

public record ToolResult(
    string Content,
    string ShortContent,
    bool Error = false)
{
    public IoOperation[] IoOperations { get; set; } = [];
    public ToolResultFlags Flags { get; set; } = new();
}