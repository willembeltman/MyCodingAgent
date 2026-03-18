using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.AgentCommunication;

public class DebuggerNeedsProjectManagerAnswer_Tool(Workspace workspace) : IToolCall
{
    public string Name
        => "answer_debug_question";

    public string Description
        => "Provides the official response or missing technical details to a Coding Agent request";

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

        if (workspace.DebugAgent_To_ProjectManagerAgent_Question == null)
            throw new Exception("Oh ooh..");

        var coderToolCall = workspace.CodingHistory
            .SelectMany(a => a.ToolCallResults)
            .FirstOrDefault(a => a.tool_call.id == workspace.DebugAgent_To_ProjectManagerAgent_Question.ToolCallId);
        if (coderToolCall == null)
            throw new Exception("Oh ooh..");

        coderToolCall.result.content = toolArguments.content;
        workspace.DebugAgent_To_ProjectManagerAgent_Question = null;

        return new ToolResult(
            $"Updated subtask '{toolArguments.id}'",
            $"Updated subtask",
            false);
    }
}