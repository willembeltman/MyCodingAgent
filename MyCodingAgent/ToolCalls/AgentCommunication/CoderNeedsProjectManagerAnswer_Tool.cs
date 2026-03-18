using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

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

    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.content == null)
            return new ToolResult(
                "parameter content is not supplied",
                "parameter content is not supplied",
                true);

        if (workspace.CodingAgent_To_ProjectManagerAgent_Question == null)
            throw new Exception("Oh ooh..");

        var coderToolCall = workspace.CodingHistory
            .SelectMany(a => a.ToolCallResults)
            .FirstOrDefault(a => a.tool_call.id == workspace.CodingAgent_To_ProjectManagerAgent_Question.ToolCallId);
        if (coderToolCall == null)
            throw new Exception("Oh ooh..");

        coderToolCall.result.content = toolArguments.content;
        workspace.CodingAgent_To_ProjectManagerAgent_Question = null;

        return new ToolResult(
            $"Updated subtask '{toolArguments.id}'",
            $"Updated subtask",
            false);
    }
}