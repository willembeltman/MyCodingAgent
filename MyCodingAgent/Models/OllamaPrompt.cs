namespace MyCodingAgent.Models;

public record OllamaPrompt(
    OllamaMessage[] messages,
    Tool[] tools);
