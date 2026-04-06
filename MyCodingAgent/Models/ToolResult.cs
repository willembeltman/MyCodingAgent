namespace MyCodingAgent.Models;

public record ToolResult(
    string Content,
    string ShortContent,
    IoOperation[] IoOperations,
    WorkspaceEventFlags Flags,
    bool Error = false);

