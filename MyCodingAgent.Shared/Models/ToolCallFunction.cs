namespace MyCodingAgent.Shared.Models;

public record ToolCallFunction(
    string name,
    ToolCallFunctionArguments arguments);