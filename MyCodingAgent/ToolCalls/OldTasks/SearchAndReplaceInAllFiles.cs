using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using System.Text.RegularExpressions;

namespace MyCodingAgent.ToolCalls.OldTasks;

public class SearchAndReplaceInAllFiles(Workspace workspace) : IToolCall
{
    public string Name
    => "search_and_replace_in_all_files";

    public string Description
        => "A global refactoring tool that replaces every occurrence of a string across all files in the workspace. Use this with extreme caution for project-wide renames (e.g., namespaces or shared constants). Always verify the search string is unique enough to avoid accidental changes in unrelated files.";

    public ToolParameter[] Parameters { get; } =
    [
        new ("query", "string", "The exact, case-sensitive string to find. Ensure this is specific enough to avoid false positives."),
        new ("replaceText", "string", "The exact, case-sensitive string to insert as a replacement.")
    ];

    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.query == null)
            return new ToolResult(
                "parameter query is not supplied.",
                "parameter query is not supplied.",
                true);
        if (toolArguments.replaceText == null)
            return new ToolResult(
                "parameter replaceText is not supplied.",
                "parameter replaceText is not supplied.",
                true);

        var allChanges = 0;
        foreach (var file in workspace.Files)
        {
            var content = await file.GetFileContent();
            var fileChanges = Regex.Matches(content, Regex.Escape(toolArguments.query)).Count;
            content = content.Replace(toolArguments.query, toolArguments.replaceText);

            if (fileChanges > 0)
            {
                await file.UpdateContent(content);
            }
            allChanges += fileChanges;
        }

        return new ToolResult(
            $"Replaced {allChanges} instances",
            $"Replaced {allChanges} instances",
            false);
    }
}