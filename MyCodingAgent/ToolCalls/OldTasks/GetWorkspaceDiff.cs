using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class GetWorkspaceDiff(Workspace workspace) : IToolCall
{
    public string Name
    => "get_workspace_diff";

    public string Description
        => "Returns a standard unified diff of all uncommitted changes in the workspace. Use this to review the actual code modifications made by coding agents, ensuring they follow the plan and haven't introduced errors.";

    public ToolParameter[] Parameters { get; } = [];

    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        var bla = workspace;

        throw new NotImplementedException();
        //return new ToolResult("Ja en nu?", "Ja en nu? (kort)", false);
    }
}
