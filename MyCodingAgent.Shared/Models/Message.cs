namespace MyCodingAgent.Shared.Models;

public record Message(
    string Role,
    string? ToolCallId,
    string? Content,
    string? Thinking,
    ToolCall[]? ToolCalls);
