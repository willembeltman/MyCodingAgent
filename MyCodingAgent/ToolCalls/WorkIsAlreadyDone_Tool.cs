using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls;

public class WorkIsAlreadyDone_Tool(Workspace workspace) : IToolCall
{
    public string Name
        => "work_is_already_done";
    public string Description
        => "Use this tool to signal all required work is already done";
    public ToolParameter[] Parameters { get; } = [];

    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        workspace.Flags.PlanningIsDoneFlag = true;
        workspace.Flags.IsCodeReviewingFlag = true;
        await workspace.Save();
        return new ToolResult("OK DONE!", "OK DONE!", false);
    }
}
