using MyCodingAgent.Enums;

namespace MyCodingAgent.Models;

public record WorkspaceInboxMessage(
    string ToolCallId,
    AgentType From,
    AgentType To,
    string Question);