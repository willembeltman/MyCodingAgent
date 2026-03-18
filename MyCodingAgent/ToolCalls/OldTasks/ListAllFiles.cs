using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class ListAllFiles(Workspace workspace) : IToolCall
{
    public string Name
        => "list_all_files";
    public string Description
        => "Returns a flat list of all file paths with line count in the workspace. Use this to discover your project structure.";
    public ToolParameter[] Parameters { get; } = [];
    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        var listAllFilesText = await workspace.GetListAllFilesText();
        return new ToolResult(listAllFilesText, "Shown all files", false);
    }
}
