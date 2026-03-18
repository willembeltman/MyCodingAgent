namespace MyCodingAgent.Models;

public record OllamaToolCall(
    string id,
    OllamaToolCallFunction function);
