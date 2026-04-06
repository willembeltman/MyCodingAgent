using MyCodingAgent.Enums;

namespace MyCodingAgent.Models;

public record WorkspaceInboxMessage(
    string ToolCallId,
    Actor From,
    Actor To,
    string Question);