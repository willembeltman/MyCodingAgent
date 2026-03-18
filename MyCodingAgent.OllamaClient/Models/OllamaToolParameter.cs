namespace MyCodingAgent.OllamaClient.Models;

internal record OllamaToolParameter(
    string Name,
    string Type,
    string Description,
    string[]? Enum = null,
    bool Optional = false);