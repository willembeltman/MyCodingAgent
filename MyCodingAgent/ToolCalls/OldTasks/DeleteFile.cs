using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class DeleteFile(Workspace workspace) : IToolCall
{
    public string Name
    => "delete_file";

    public string Description
        => "Removes a file from the workspace. Use this only when a file is no longer needed or was created by mistake.";

    public ToolParameter[] Parameters { get; } =
    [
        new ("path", "string", "The relative path of the file to be deleted (e.g., 'old_logic.cs').")
    ];
    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.path == null)
            return new ToolResult(
                "parameter path is not supplied.",
                "parameter path is not supplied.",
                true);

        workspace.GaurdParseFullPath(toolArguments.path, out var fullPath);

        try
        {
            var file = workspace.GetFile(toolArguments.path);
            if (file != null)
            {
                file.Delete();
                workspace.Files.Remove(file);
                return new ToolResult(
                    $"Deleted file {toolArguments.path}",
                    $"Deleted file",
                    false);
            }
            return new ToolResult(
                $"Error while deleting file '{toolArguments.path}': could not find file",
                $"Error while deleting file: could not find",
                true);
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error while deleting file '{toolArguments.path}': {ex.Message}",
                $"Error while deleting file",
                true);
        }
    }
}