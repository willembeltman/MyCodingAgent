using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class CreateSubTask(Workspace workspace) : IToolCall
{
    public string Name
     => "create_subtask";

    public string Description
        => "Creates a new subtask for the current project. Use this to break down the user request into smaller development steps.";

    public ToolParameter[] Parameters { get; } =
    [
        new ("content", "string", "The full description of the task, including technical details and what needs to be changed.")
    ];

    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.content == null)
            return new ToolResult(
                "parameter content is not supplied.",
                "parameter content is not supplied.",
                true);
        try
        {
            var id = workspace.SubTasks.Any() ? workspace.SubTasks.Max(a => a.Id) : 0;
            var newSubTask = new WorkspaceSubTask(++id, toolArguments.content);
            workspace.SubTasks.Add(newSubTask);
            return new ToolResult(
                $"Created {toolArguments.id}",
                $"Created {toolArguments.id}",
                false);
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error while updating '{toolArguments.id}': {ex.Message}",
                $"Error while updating",
                true);
        }
    }
}