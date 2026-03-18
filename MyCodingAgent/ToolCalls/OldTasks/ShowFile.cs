using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using System.Text;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class ShowFile(Workspace workspace) : IToolCall
{
    public string Name
    => "show_file";

    public string Description
        => "Reads and returns the full raw content of a file. Use this to understand existing code, review logic, or gather context before planning sub-tasks. For precise editing, consider using show_file_with_linenumbers instead.";

    public ToolParameter[] Parameters { get; } =
    [
        new ("path", "string", "The relative path to the file you want to read (e.g., 'src/api/auth.js').")
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
            return new ToolResult(
                $"Error opening file '{toolArguments.path}': file not found",
                $"Error opening file '{toolArguments.path}': file not found",
                true);
        }

        //StringBuilder sb = new StringBuilder();
        //sb.AppendLine(file.RelativePath);
        var fileContent = await file.GetFileContent();
        //foreach (var line in fileContent.GetLines())
        //{
        //    sb.AppendLine($"{line.lineNumber,3}|{line.content}");
        //}

        return new ToolResult(
            fileContent,
            //sb.ToString(),
            $"Showed file '{toolArguments.path}'",
            false);
    }
}