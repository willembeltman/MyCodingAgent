using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class MoveFile(Workspace workspace) : IToolCall
{
    public string Name
        => "move_file";
    public string Description
        => "move or rename a file";
    public ToolParameter[] Parameters { get; } =
    [
        new ("path", "string", "current path of the file"),
        new ("newPath", "string", "new path of the file")
    ];
    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.path == null)
            return new ToolResult(
                "parameter path is not supplied.",
                "parameter path is not supplied.",
                true);
        if (toolArguments.newPath == null)
            return new ToolResult(
                "parameter newPath is not supplied.",
                "parameter newPath is not supplied.",
                true);

        workspace.GaurdParseFullPath(toolArguments.newPath, out var newFullPath);

        try
        {
            var file = workspace.GetFile(toolArguments.path);
            if (file != null && file.Exists())
            {
                file.Move(toolArguments.newPath, newFullPath);
                return new ToolResult(
                    $"Moved file {toolArguments.path} -> {toolArguments.newPath}",
                    $"Moved file",
                    false);
            }
            return new ToolResult(
                $"Error while moving file '{toolArguments.path}': could not find file",
                $"Error while moving file: could not find",
                true);
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error while moving file '{toolArguments.path}': {ex.Message}",
                $"Error while moving file",
                true);
        }
    }
}