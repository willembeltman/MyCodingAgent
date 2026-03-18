namespace MyCodingAgent.OllamaClient;

public record OllamaToolCall(
    string id,
    OllamaToolCallFunction function);
