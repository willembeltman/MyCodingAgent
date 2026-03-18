namespace MyCodingAgent.OllamaClient.Models;

internal record OllamaPrompt(
    OllamaMessage[] messages,
    OllamaTool[] tools);
