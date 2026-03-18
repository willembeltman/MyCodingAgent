using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.AgentCommunication;

public class DebuggerNeedsCoderAnswer_Tool(Workspace workspace) : IToolCall
{
    public string Name
        => "answer_debug_question";

    public string Description
        => "Provide the official response or missing technical details to a Debug Agent question";

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

        if (workspace.DebugAgent_To_CoderAgent_Question == null)
            throw new Exception("Oh ooh..");

        var debugToolCall = workspace.DebugHistory
            .SelectMany(a => a.ToolCallResults)
            .FirstOrDefault(a => a.tool_call.id == workspace.DebugAgent_To_CoderAgent_Question.ToolCallId);
        if (debugToolCall == null)
            throw new Exception("Oh ooh..");

        debugToolCall.result.content = toolArguments.content;
        workspace.DebugAgent_To_CoderAgent_Question = null;

        return new ToolResult(
            $"Updated subtask '{toolArguments.id}'",
            $"Updated subtask",
            false);
    }
}