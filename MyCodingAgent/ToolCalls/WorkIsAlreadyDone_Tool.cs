using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls;

public class WorkIsAlreadyDone_Tool(Current current) : IToolCall
{
    public string Name
        => "work_is_already_done";
    public string Description
        => "Use this tool to signal all required work is already done";
    public ToolParameter[] Parameters { get; } = [];

    public async Task<ToolResult> Invoke(ToolCall toolCall)
    {
        current.Task.Flags.PlanningIsDoneFlag = true;
        current.Task.Flags.IsCodeReviewingFlag = true;
        await current.Save();
        return new ToolResult("OK DONE!", "OK DONE!", false);
    }
}
