namespace MyCodingAgent.OllamaClient.Models;

internal record OllamaToolCall(
    string id,
    OllamaToolCallFunction function);
