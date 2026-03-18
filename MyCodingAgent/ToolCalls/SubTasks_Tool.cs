using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls;

public class SubTasks_Tool(Workspace workspace) : IToolCall
{
    public string Name => "subtasks";

    public string Description
        => "Shows the full plan with sub-tasks, status, and IDs; use it to track progress or review before changes, use planning_is_done action to signal readiness to build";
    public ToolParameter[] Parameters { get; } = [
        new ("action", "string", "Action to perform", ["list_all", "create", "update", "delete", "planning_is_done"]),
        new ("id", "number", "The numerical ID of the sub-task to be updated (used in 'update' and 'delete' action)", null, true),
        new ("content", "string", "The text content for the sub-task (used in 'create' and 'update' action). Provide the full updated description to ensure clarity for the coding agents", null, true)
    ];
    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.action == null)
            return new ToolResult(
                "parameter action is not supplied",
                "parameter action is not supplied",
                true);


        return toolArguments.action.ToLower() switch
        {
            "list_all" => await ListAll(toolCall),
            "create" => await Create(toolCall),
            "update" => await Update(toolCall),
            "delete" => await Delete(toolCall),
            "planning_is_done" => await PlanningIsDone(toolCall),
            _ => new ToolResult(
                $"Error could not find action '{toolArguments.action}'",
                $"Error could not find action '{toolArguments.action}'",
                true)
        };
    }

    public async Task<ToolResult> ListAll(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        var listAllSubTasksText = await workspace.GetListAllSubTasksText();
        return new ToolResult(listAllSubTasksText, "Shown all subtasks", false);
    }
    public async Task<ToolResult> Create(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.content == null)
            return new ToolResult(
                "parameter content is not supplied",
                "parameter content is not supplied",
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
    public async Task<ToolResult> Update(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.id == null)
            return new ToolResult(
                "parameter id is not supplied",
                "parameter id is not supplied",
                true);
        if (toolArguments.content == null)
            return new ToolResult(
                "parameter content is not supplied",
                "parameter content is not supplied",
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
    public async Task<ToolResult> Delete(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.id == null)
            return new ToolResult(
                "parameter id is not supplied",
                "parameter id is not supplied",
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
    public async Task<ToolResult> PlanningIsDone(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        workspace.Flags.PlanningIsDoneFlag = true;
        return new ToolResult("OK DONE!", "OK DONE!", false);
    }
}
