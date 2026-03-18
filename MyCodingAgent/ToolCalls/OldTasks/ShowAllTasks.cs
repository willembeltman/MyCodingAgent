using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class ShowAllSubTasks(Workspace workspace) : IToolCall
{
    public string Name
    => "show_all_subtasks";

    public string Description
        => "Returns a complete overview of the current plan, including all sub-tasks, their status, and unique IDs. Use this to track progress, identify remaining work, or before making changes to the task list.";
    public ToolParameter[] Parameters { get; } = [];
    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        var listAllSubTasksText = await workspace.GetListAllSubTasksText();
        return new ToolResult(listAllSubTasksText, "Shown all subtasks", false);
    }
}
