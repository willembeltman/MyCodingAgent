using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.AgentCommunication;

public class DebuggerNeedsProjectManager_Tool(Workspace workspace) : IToolCall
{
    public string Name
        => "ask_project_manager_agent";

    public string Description
        => "Ask the project manager for clarification or missing details";

    public ToolParameter[] Parameters { get; } =
    [
        new ("content", "string", "Question or missing information")
    ];

    public async Task<ToolResult> Invoke(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Content == null)
            return new ToolResult(
                "parameter content is not supplied",
                "parameter content is not supplied",
                true);

        if (toolCall.Id == null)
            throw new Exception("eeeuhm..");

        workspace.DebugAgent_To_ProjectManagerAgent_Question =
            new(toolCall.Id, toolArguments.Content);

        var answer = "Waiting for answer..";
        return new ToolResult(
            answer,
            answer,
            false);
    }
}