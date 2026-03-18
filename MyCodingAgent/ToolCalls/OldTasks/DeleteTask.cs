using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class DeleteSubTask(Workspace workspace) : IToolCall
{
    public string Name
    => "delete_subtask";

    public string Description
        => "Removes a planned sub-task from the project queue. Use this if a task was created in error, becomes redundant, or if the plan needs to be restructured before execution. Avoid deleting tasks that are currently being processed by a coding agent.";

    public ToolParameter[] Parameters { get; } =
    [
        new ("id", "string", "The unique identifier of the sub-task to be removed (e.g., 'task_01').")
    ];

    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.id == null)
            return new ToolResult(
                "parameter id is not supplied.",
                "parameter id is not supplied.",
                true);

        try
        {
            var subtask = workspace.GetSubTask(toolArguments.id);
            if (subtask != null)
            {
                workspace.SubTasks.Remove(subtask);
                return new ToolResult(
                    $"Deleted subtask {toolArguments.id}",
                    $"Deleted subtask",
                    false);
            }
            return new ToolResult(
                $"Error while deleting subtask '{toolArguments.id}': could not find subtask",
                $"Error while deleting subtask: could not find",
                true);
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error while deleting subtask '{toolArguments.id}': {ex.Message}",
                $"Error while deleting subtask",
                true);
        }
    }
}