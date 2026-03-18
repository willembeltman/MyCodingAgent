namespace MyCodingAgent.OllamaClient.Models;

internal record OllamaMessage(
    string role,
    string? tool_call_id,
    string? content,
    string? thinking,
    OllamaToolCall[]? tool_calls);
