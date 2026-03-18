using MyCodingAgent.Shared.Models;

namespace MyCodingAgent.Models;

public record ToolCallResult(
    ToolCall tool_call,
    ToolResult result);
