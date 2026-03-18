using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class UpdateSubTask(Workspace workspace) : IToolCall
{
    public string Name
      => "update_subtask";

    public string Description
        => "Modifies an existing sub-task by overwriting its content. Use this to refine task requirements or add technical notes discovered by the debugger. Note: This action replaces the previous task description with the new content.";

    public ToolParameter[] Parameters { get; } =
    [
        new ("id", "number", "The numerical ID of the sub-task to be updated."),
        new ("content", "string", "The new text content for the sub-task. Provide the full updated description to ensure clarity for the coding agents.")
    ];

    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.id == null)
            return new ToolResult(
                "parameter id is not supplied.",
                "parameter id is not supplied.",
                true);
        if (toolArguments.content == null)
            return new ToolResult(
                "parameter content is not supplied.",
                "parameter content is not supplied.",
                true);

        var subtask = workspace.GetSubTask(toolArguments.id);
        if (subtask == null)
            return new ToolResult(
                $"Error could not find subtask {toolArguments.id}",
                $"Error could not find subtask",
                true);

        workspace.SubTasks.Remove(subtask);
        var newSubTask = new WorkspaceSubTask(subtask.Id, toolArguments.content);
        workspace.SubTasks.Add(newSubTask);
        return new ToolResult(
            $"Updated subtask '{toolArguments.id}'",
            $"Updated subtask",
            false);
    }
}