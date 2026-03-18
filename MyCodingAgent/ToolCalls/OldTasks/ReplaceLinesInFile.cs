using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class ReplaceLinesInFile(Workspace workspace) : IToolCall
{
    public string Name
        => "replace_lines_in_file";

    public string Description
        => "Replaces a range of lines from 'startLine' to 'endLine' (inclusive) with new content. Use this to modify existing logic. To delete lines, provide an empty string as content.";

    public ToolParameter[] Parameters { get; } =
    [
        new ("path", "string", "Path to the file."),
        new ("startLine", "number", "The first line to be replaced (1-based)."),
        new ("endLine", "number", "The last line to be replaced (inclusive)."),
        new ("content", "string", "The new code or text that will occupy this range.")
    ];

    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.path == null)
            return new ToolResult(
                "parameter path is not supplied.",
                "parameter path is not supplied.",
                true);
        if (toolArguments.startLine == null)
            return new ToolResult(
                "parameter startLine is not supplied.",
                "parameter startLine is not supplied.",
                true);
        if (toolArguments.endLine == null)
            return new ToolResult(
                "parameter endLine is not supplied.",
                "parameter endLine is not supplied.",
                true);
        if (toolArguments.content == null)
            return new ToolResult(
                "parameter content is not supplied.",
                "parameter content is not supplied.",
                true);

        var file = workspace.GetFile(toolArguments.path);
        if (file == null)
            return new ToolResult(
                $"Error could not find file '{toolArguments.path}'",
                $"Error could not find file",
                true);

        try
        {
            await file.UpdateContent(
                toolArguments.startLine.Value , 
                toolArguments.endLine.Value, 
                toolArguments.content);
            return new ToolResult(
                $"Updated file '{toolArguments.path}'",
                $"Updated file",
                false);
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error while updating file '{toolArguments.path}': {ex.Message}",
                $"Error while updating file",
                true);
        }
    }
}