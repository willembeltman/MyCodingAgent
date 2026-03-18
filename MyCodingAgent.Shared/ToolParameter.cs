namespace MyCodingAgent.Shared;

public record ToolParameter(
    string Name,
    string Type,
    string Description,
    string[]? Enum = null,
    bool Optional = false);