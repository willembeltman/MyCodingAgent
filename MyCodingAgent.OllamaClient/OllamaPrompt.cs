namespace MyCodingAgent.OllamaClient;

public record OllamaPrompt(
    OllamaMessage[] messages,
    OllamaTool[] tools);
