using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls;

public class SubTaskIsFinished_Tool(Workspace workspace) : IToolCall
{
    public string Name
        => "subtask_is_done";
    public string Description
        => "Call when the current subtask is fully completed and verified, with no remaining work";
    public ToolParameter[] Parameters { get; } =
    [
    ];

    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;

        var subtask = workspace.GetCurrentSubTask();
        if (subtask == null)
            throw new Exception("wtf?");

        try
        {
            if (subtask.Finished)
            {
                return new ToolResult(
                    $"Error subtask '{toolArguments.id}' already finished",
                    $"Error subtask already finished",
                    true);
            }

            subtask.Finished = true;
            workspace.Flags.NeedClearCodingHistoryFlag = true;

            return new ToolResult(
                $"Finished subtask '{subtask.Id}'",
                $"Finished subtask",
                false);
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error exception while finishing subtask '{subtask.Id}': {ex.Message}",
                $"Error exception while finishing subtask ",
                true);
        }
    }
}