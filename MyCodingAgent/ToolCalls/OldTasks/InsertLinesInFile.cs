using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class InsertLinesInFile(Workspace workspace) : IToolCall
{
    public string Name
        => "insert_lines_into_file";

    public string Description
        => "Inserts new lines at a specific position in a file. All existing lines from 'startLine' onwards are shifted down. Use this to add new methods, imports, or logic without overwriting existing code.";

    public ToolParameter[] Parameters { get; } =
    [
        new ("path", "string", "Path to the file."),
        new ("startLine", "number", "The line number where the new content should begin (1-based)."),
        new ("content", "string", "The text to insert. Ensure proper indentation matches the surrounding code.")
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
                toolArguments.startLine.Value,
                -1, 
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