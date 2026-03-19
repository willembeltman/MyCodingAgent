using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.AgentCommunication;

public class CoderNeedsProjectManager_Tool(Workspace workspace) : IToolCall
{
    public string Name
        => "ask_project_manager_agent";

    public string Description
        => "Use when blocked by missing info or unclear requirements";

    public ToolParameter[] Parameters { get; } =
    [
        new ("content", "string", "The specific question or missing information needed to proceed with the task")
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

        workspace.CodingAgent_To_ProjectManagerAgent_Question =
            new(toolCall.Id, toolArguments.Content);

        var answer = "Waiting for answer..";
        return new ToolResult(
            answer,
            answer,
            false);
    }
}