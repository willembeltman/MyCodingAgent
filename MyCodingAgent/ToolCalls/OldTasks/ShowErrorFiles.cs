using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using System.Text;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class ShowErrorFiles(Workspace workspace) : IToolCall
{
    public string Name
        => "show_error_files";

    public string Description
        => "give a overview of all errors with the source code of the correspondending files";

    public ToolParameter[] Parameters { get; } =
    [
    ];

    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"ERRORS (GROUPED BY FILES)");
        sb.AppendLine();
        var compileResult = await workspace.Compile();
        foreach (var fileGroup in compileResult.Errors.GroupBy(a => a.File))
        {
            var relativePath = fileGroup.Key;
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                sb.AppendLine("FILE: <null>");
                foreach (var error in fileGroup)
                {
                    sb.AppendLine(error.FullError.TrimEnd('\n').TrimEnd('\r'));
                }
                sb.AppendLine();
                continue;
            }

            var file = workspace.GetFile(relativePath);
            if (file != null)
            {
                sb.AppendLine($"FILE: {relativePath}");
                sb.AppendLine("CODE");
                var fileContent = await file.GetFileContent();
                foreach (var line in fileContent.GetLines())
                {
                    sb.AppendLine($"{line.lineNumber,3}|{line.content}");
                }
                sb.AppendLine();
                sb.AppendLine("ERRORS");
                foreach (var error in fileGroup)
                {
                    sb.AppendLine(error.FullError.TrimEnd('\n').TrimEnd('\r'));
                }
                sb.AppendLine();
            }
        }

        return new ToolResult(
            sb.ToString(),
            compileResult.Content,
            false);
    }
}