using MyCodingAgent.Models;

namespace MyCodingAgent.Models;

public record ToolCallResult(
    ToolCall ToolCall,
    ToolResult Result);
