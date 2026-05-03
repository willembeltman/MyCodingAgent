using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls;

public class WorkIsDone_Tool(Current current) : IToolCall
{
    public string Name
        => "work_is_done";
    public string Description
        => "Use this tool to submit final results once all sub-tasks are complete and verified, and the workspace is stable";
    public ToolParameter[] Parameters { get; } = [];
    public async Task<ToolResult> Invoke(ToolCall toolCall)
    {
        current.Task.Flags.IsCodeReviewingFlag = true;
        await current.Save();
        return new ToolResult("OK DONE!", "OK DONE!", false);
    }
}
