using MyCodingAgent.Shared.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Shared.Models;

namespace MyCodingAgent.ToolCalls.AgentCommunication;

public class DebuggerNeedsCoder_Tool(Workspace workspace) : IToolCall
{
    public string Name
        => "ask_coder_agent";

    public string Description
        => "Ask the coder agent for clarification or missing details";

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

        workspace.DebugAgent_To_CoderAgent_Question =
            new(toolCall.Id, toolArguments.Content);

        var answer = "Waiting for answer..";
        return new ToolResult(
            answer,
            answer,
            false);
    }
}