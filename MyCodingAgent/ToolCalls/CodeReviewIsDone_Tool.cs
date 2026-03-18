using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls;

public class CodeReviewIsDone_Tool(Workspace workspace) : IToolCall
{
    public string Name
        => "codereview_is_done";
    public string Description
        => "Use this tool signal your are done with the code review. If you call this after you created new subtasks, development will continue. If you call this without new subtasks, the project will be marked as done.";
    public ToolParameter[] Parameters { get; } = []; // parameters
    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        workspace.Flags.IsCodeReviewingFlag = false;
        workspace.Flags.WorkIsDoneFlag = !workspace.SubTasks.Any(a => a.Finished == false);
        await workspace.Save();
        if (workspace.Flags.WorkIsDoneFlag)
        {
            return new ToolResult("Code review approved", "Code review approved", false);
        }
        else
        {
            return new ToolResult("Code review denied", "Code review denied", false);
        }
    }
}
