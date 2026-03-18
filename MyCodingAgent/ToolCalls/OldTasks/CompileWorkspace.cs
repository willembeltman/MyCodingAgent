using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class CompileWorkspace(Workspace workspace) : IToolCall
{
    public string Name 
        => "get_compilation_result";
    public string Description
        => "Builds the solution/project(s) using the .sln, .slnx or .csproj file in your workspace root. Use this to catch errors after making code changes.";
    public ToolParameter[] Parameters { get; } = [];

    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        var compileResult = await workspace.Compile();
        return new ToolResult(
            compileResult.Content,
            compileResult.Errors.Count > 0 ? "Compiled with errors" : "Compiled succesfully",
            compileResult.Errors.Count > 0);
    }
}
