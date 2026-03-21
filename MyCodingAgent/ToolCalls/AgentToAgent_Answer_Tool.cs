using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls;

public class AgentToAgent_Answer_Tool(
    Workspace workspace,
    string name,
    string description,
    string contentParameterDescription)
    : IToolCall
{
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
    public ToolParameter[] Parameters { get; } =
    [
        new ("content", "string", contentParameterDescription)
    ];
    public WorkspaceInboxMessage? Message { get; set; }

    public void SetCurrentMessage(WorkspaceInboxMessage? message)
    {
        Message = message;
    }
    public async Task<ToolResult> Invoke(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Content == null)
            return new ToolResult(
                "parameter content is not supplied",
                "parameter content is not supplied",
                true);

        if (Message == null)
            throw new Exception("Oh ooh..");

        var coderToolCall = workspace.History
            .Where(a => a.ResponseResults != null)
            .SelectMany(a => a.ResponseResults!.ToolCallResults)
            .FirstOrDefault(a => a.tool_call.Id == Message.ToolCallId)
            ?? throw new Exception("Oh ooh..");
        coderToolCall.result.content = toolArguments.Content;

        workspace.InboxMessages.Remove(Message);
        Message = null;

        return new ToolResult(
            $"Updated subtask '{toolArguments.Id}'",
            $"Updated subtask",
            false);
    }
}