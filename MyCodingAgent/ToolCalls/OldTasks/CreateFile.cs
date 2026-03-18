using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class CreateFile(Workspace workspace) : IToolCall
{
    public string Name
    => "create_file";

    public string Description
        => "Create a new file at the specified path with the provided content. If the file already exists, it will be overwritten. Use this for initial file creation or complete rewrites.";

    public ToolParameter[] Parameters { get; } =
    [
        new ("path", "string", "The relative path from the workspace root where the file should be created (e.g., 'src/main.cs')."),
        new ("content", "string", "The complete source code or text content to be written into the new file.")
    ];
    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.path == null)
            return new ToolResult(
                "parameter path is not supplied.",
                "parameter path is not supplied.",
                true);
        if (toolArguments.content == null)
            return new ToolResult(
                "parameter content is not supplied.",
                "parameter content is not supplied.",
                true);

        workspace.GaurdParseFullPath(toolArguments.path, out var fullPath);

        try
        {
            var file = workspace.GetFile(toolArguments.path);
            if (file == null)
            {
                var newFile = new WorkspaceFile(toolArguments.path, fullPath);
                await newFile.UpdateContent(toolArguments.content);
                workspace.Files.Add(newFile);
                return new ToolResult(
                    $"Created {toolArguments.path}",
                    $"Created {toolArguments.path}",
                    false);
            }
            else
            {
                //await file.UpdateContent(toolArguments.content);
                //return new ToolResult(
                //    $"Updated {toolArguments.path}",
                //    $"Updated {toolArguments.path}",
                //    false);
                return new ToolResult(
                    $"Error while updating '{toolArguments.path}': file already exists",
                    $"Error while updating",
                    true);
            }
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error while updating '{toolArguments.path}': {ex.Message}",
                $"Error while updating",
                true);
        }
    }
}