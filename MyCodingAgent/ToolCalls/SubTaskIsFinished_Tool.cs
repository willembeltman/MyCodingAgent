using MyCodingAgent.Extentions;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls;

public class SubTaskIsFinished_Tool(Current current) : IToolCall
{
    public string Name
        => "subtask_is_done";
    public string Description
        => "Call when the current subtask is fully completed and verified, with no remaining work";
    public ToolParameter[] Parameters { get; } =
    [
    ];

    public async Task<ToolResult> Invoke(ToolCall toolCall)
    {
        var subtask = current.SubTask
            ?? throw new Exception("wtf?");

        if (subtask.Finished)
        {
            return new ToolResult(
                $"Error subtask '{toolCall.Function.Arguments.Id}' already finished",
                $"Error subtask already finished",
                true);
        }
        subtask.Finished = true; // onnodig maar duidelijk

        return new ToolResult(
            $"Finished subtask '{subtask.Id}'",
            $"Finished subtask",
            false)
        {
            Flags = new ToolResultFlags() { SubTaskIsDoneFlag = true }
        };
    }
}