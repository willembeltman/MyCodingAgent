namespace MyCodingAgent.Shared;

public record Message(
    string role,
    string? tool_call_id,
    string? content,
    string? thinking,
    ToolCall[]? tool_calls);
