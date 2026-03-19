using MyCodingAgent.Shared.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Shared.Models;

namespace MyCodingAgent.ToolCalls.AgentCommunication;

public class CoderNeedsProjectManagerAnswer_Tool(Workspace workspace) : IToolCall
{
    public string Name
        => "answer_coder_question";

    public string Description
        => "Provide the official response or missing technical details to a Coding Agents question";

    public ToolParameter[] Parameters { get; } =
    [
        new ("content", "string", "The detailed answer or instruction that will be sent back to the coding agent")
    ];

    public async Task<ToolResult> Invoke(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Content == null)
            return new ToolResult(
                "parameter content is not supplied",
                "parameter content is not supplied",
                true);

        if (workspace.CodingAgent_To_ProjectManagerAgent_Question == null)
            throw new Exception("Oh ooh..");

        var coderToolCall = workspace.CodingHistory
            .SelectMany(a => a.ToolCallResults)
            .FirstOrDefault(a => a.tool_call.Id == workspace.CodingAgent_To_ProjectManagerAgent_Question.ToolCallId)
            ?? throw new Exception("Oh ooh..");
        coderToolCall.result.content = toolArguments.Content;
        workspace.CodingAgent_To_ProjectManagerAgent_Question = null;

        return new ToolResult(
            $"Updated subtask '{toolArguments.Id}'",
            $"Updated subtask",
            false);
    }
}