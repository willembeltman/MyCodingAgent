namespace MyCodingAgent.OllamaClient;

public record OllamaToolParameter(
    string Name,
    string Type,
    string Description,
    string[]? Enum = null,
    bool Optional = false);