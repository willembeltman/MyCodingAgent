using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls;

public class DebuggingIsDone_Tool(Workspace workspace) : IToolCall
{
    public string Name
        => "debug_is_done";
    public string Description
        => "The definitive signal that all bugs are fixed and verified, use this to submit final results to the coding agent";
    public ToolParameter[] Parameters { get; } = 
    [
        new ("content", "string", "Review of your fixes", null, true)
    ];
    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.content == null)
            return new ToolResult(
                "Error parameter content is not supplied",
                "Error parameter content is not supplied",
                true);

        var history = workspace.CodingHistory.LastOrDefault();
        if (history != null)
        {
            history.ToolCallResults.Add(
                new ToolCallResult(toolCall,
                    new ToolResult(
                        $"Your changes resulted in a error, so the debug agent has fixed them.\r\nThis is his rapport about the fix:\r\n{toolArguments.content}",
                        $"Your changes resulted in a error, so the debug agent has fixed them",
                        false)));
        }
        workspace.Flags.IsDebuggingFlag = false;
        workspace.Flags.NeedClearDebugHistoryFlag = true;
        await workspace.Save();

        return new ToolResult("OK DONE!", "OK DONE!", false);
    }
}
