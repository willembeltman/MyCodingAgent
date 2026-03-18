using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class GetFileDiff(Workspace workspace) : IToolCall
{
    public string Name
    => "get_workspace_diff";

    public string Description
        => "Returns a standard unified diff of all uncommitted changes in the workspace. Use this to review the actual code modifications made by coding agents, ensuring they follow the plan and haven't introduced errors.";

    public ToolParameter[] Parameters { get; } = 
    [
        new ("path", "string", "The relative path from the workspace root where the file should be created (e.g., 'src/main.cs').")
    ];

    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.path == null)
            return new ToolResult(
                "parameter path is not supplied.",
                "parameter path is not supplied.",
                true);

        var file = workspace.GetFile(toolArguments.path);
        if (file == null)
        {
        }

        throw new NotImplementedException();
        //return new ToolResult("Ja en nu?", "Ja en nu? (kort)", false);
    }
}
