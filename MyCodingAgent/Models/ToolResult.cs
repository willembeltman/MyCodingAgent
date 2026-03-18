namespace MyCodingAgent.Models;

public class ToolResult
{
    public ToolResult()
    {
    }
    public ToolResult(
        string content,
        string shortContent,
        bool error)
    {
        this.content = content;
        this.shortContent = shortContent;
        this.error = error;
    }
    public string? content { get; set; }
    public string? shortContent { get; set; } 
    public bool? error { get; set; }
}