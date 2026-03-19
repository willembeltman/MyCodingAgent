using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
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
    public async Task<ToolResult> Invoke(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Action == null)
            return new ToolResult(
                "parameter action is not supplied",
                "parameter action is not supplied",
                true);


        return toolArguments.Action.ToLower() switch
        {
            "list" => await List(),
            "list_all" => await List(),
            "create" => await Create(toolCall),
            "update" => await Update(toolCall),
            "remove" => await Delete(toolCall),
            "delete" => await Delete(toolCall),
            "planning_is_done" => await PlanningIsDone(),
            _ => new ToolResult(
                $"Error could not find action '{toolArguments.Action}'",
                $"Error could not find action '{toolArguments.Action}'",
                true)
        };
    }

    public async Task<ToolResult> List()
    {
        var listAllSubTasksText = await workspace.GetListAllSubTasksText();
        return new ToolResult(listAllSubTasksText, "Shown all subtasks", false);
    }
    public async Task<ToolResult> Create(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Content == null)
            return new ToolResult(
                "parameter content is not supplied",
                "parameter content is not supplied",
                true);
        try
        {
            var id = workspace.SubTasks.Count != 0 ? workspace.SubTasks.Max(a => a.Id) : 0;
            var newSubTask = new WorkspaceSubTask(++id, toolArguments.Content);
            workspace.SubTasks.Add(newSubTask);
            return new ToolResult(
                $"Created {toolArguments.Id}",
                $"Created {toolArguments.Id}",
                false);
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error while updating '{toolArguments.Id}': {ex.Message}",
                $"Error while updating",
                true);
        }
    }
    public async Task<ToolResult> Update(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Id == null)
            return new ToolResult(
                "parameter id is not supplied",
                "parameter id is not supplied",
                true);
        if (toolArguments.Content == null)
            return new ToolResult(
                "parameter content is not supplied",
                "parameter content is not supplied",
                true);

        var subtask = workspace.GetSubTask(toolArguments.Id);
        if (subtask == null)
            return new ToolResult(
                $"Error could not find subtask {toolArguments.Id}",
                $"Error could not find subtask",
                true);

        workspace.SubTasks.Remove(subtask);
        var newSubTask = new WorkspaceSubTask(subtask.Id, toolArguments.Content);
        workspace.SubTasks.Add(newSubTask);
        return new ToolResult(
            $"Updated subtask '{toolArguments.Id}'",
            $"Updated subtask",
            false);
    }
    public async Task<ToolResult> Delete(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Id == null)
            return new ToolResult(
                "parameter id is not supplied",
                "parameter id is not supplied",
                true);

        try
        {
            var subtask = workspace.GetSubTask(toolArguments.Id);
            if (subtask != null)
            {
                workspace.SubTasks.Remove(subtask);
                return new ToolResult(
                    $"Deleted subtask {toolArguments.Id}",
                    $"Deleted subtask",
                    false);
            }
            return new ToolResult(
                $"Error while deleting subtask '{toolArguments.Id}': could not find subtask",
                $"Error while deleting subtask: could not find",
                true);
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error while deleting subtask '{toolArguments.Id}': {ex.Message}",
                $"Error while deleting subtask",
                true);
        }
    }
    public async Task<ToolResult> PlanningIsDone()
    {
        workspace.Flags.PlanningIsDoneFlag = true;
        return new ToolResult("OK DONE!", "OK DONE!", false);
    }
}
