using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls;

public class DebuggingIsDone_Tool(Current current) : IToolCall
{
    public string Name
        => "debug_is_done";
    public string Description
        => "The definitive signal that all bugs are fixed and verified, use this to submit final results to the coding agent";
    public ToolParameter[] Parameters { get; } = 
    [
        new ("content", "string", "Review of your fixes", null, true)
    ];
    public async Task<ToolResult> Invoke(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Content == null)
            return new ToolResult(
                "Error parameter content is not supplied",
                "Error parameter content is not supplied",
                true);

        // Geschiedenis aanmaken om de coding agent te laten weten dat hij een error heeft veroorzaakt.
        current.Workspace.Events.Add(new WorkspaceEvent()
        {
            Id = current.GetNewEventId(),
            TaskId = current.Task.Id,
            SubTaskId = current.SubTask?.Id,
            Actor = Enums.Actor.Debugger,
            Conversation = Enums.Conversation.Coding,
            Result =
                new SystemResult([
                    new SystemResultEvent(toolCall,
                        new ToolResult(
                            $"Your changes resulted in a error, so the debug agent has fixed them.\r\nThis is his rapport about the fix:\r\n{toolArguments.Content}",
                            $"Your changes resulted in a error, so the debug agent has fixed them",
                            false))])
        });

        // En de debug is done 
        return new ToolResult("Debugging done", "Debugging done", false, new ToolResultFlags() { DebuggingIsDoneFlag = true });
    }
}
