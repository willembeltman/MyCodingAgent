namespace MyCodingAgent.Models;

public record SystemResultEvent(
    ToolCall? tool_call, // is null bij veranderingen vanuit de user
    ToolResult result);
