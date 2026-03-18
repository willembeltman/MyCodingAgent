namespace MyCodingAgent.Models;

public record OllamaMessage(
    string role,
    string? tool_call_id,
    string? content,
    string? thinking,
    OllamaToolCall[]? tool_calls);
