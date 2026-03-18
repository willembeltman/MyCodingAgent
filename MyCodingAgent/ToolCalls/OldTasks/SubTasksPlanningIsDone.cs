using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class SubTasksPlanningIsDone(Workspace workspace) : IToolCall
{
    public string Name
        => "subtasks_planning_is_done";
    public string Description
        => "Finalizes the planning phase and signals that the task breakdown is complete. Use this only after all necessary sub-tasks have been created and the overall strategy is ready for execution by coding agents.";
    public ToolParameter[] Parameters { get; } = [];
    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        workspace.PlanningIsDone = true;
        await workspace.Save();
        return new ToolResult("OK DONE!", "OK DONE!", false);
    }
}
