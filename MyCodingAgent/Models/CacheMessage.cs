namespace MyCodingAgent.Models;

public record CacheMessage(
    string ToolName,
    string? Id,
    string? Action, 
    string? Path, 
    string? NewPath,
    string? Query,
    string? Content,
    int? LineNumber);