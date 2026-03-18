namespace MyCodingAgent.Models;

public record CacheMessage(
    string toolName,
    string? id,
    string? action, 
    string? path, 
    string? newPath,
    string? query,
    string? content,
    string? replaceText,
    int? lineNumber);