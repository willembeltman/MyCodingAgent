using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace MyCodingAgent.ToolCalls;

public class Workspace_Tool(Workspace Workspace) : WorkspaceReadonly_Tool(Workspace)
{
    public override string Name => "workspace";
    public override string Description => "Interact with the workspace";
    public override ToolParameter[] Parameters { get; } =
    [
        new ("action", "string", "Action to perform",
        [
            "files_list",
            "open",
            "read",
            "write",
            "append",
            "text_search",
            "text_search_and_replace",
            "delete",
            "move",
            "compile",
            "diff_with_original"
        ]),
        new ("path", "string", "File path, not used for 'files_list' action", null, true),
        new ("query", "string", "Exact text to find, for 'text_search' and 'text_search_and_replace' action", null, true),
        new ("content", "string", "Content for 'write', 'append' and 'text_search_and_replace' action", null, true),
        new ("newPath", "string", "Destination path for 'move' action", null, true),
        new ("lineNumber", "number", "Line number for 'append' action (optional)", null, true)
    ];

    public override async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.action == null)
            return new ToolResult(
                "Error parameter action is not supplied",
                "Error parameter action is not supplied",
                true);

        return toolArguments.action.ToLower() switch
        {
            "files_list" => await FilesList(toolCall),
            "open" => await Read(toolCall),
            "read" => await Read(toolCall),
            "write" => await Write(toolCall),
            "text_search" => await TextSearch(toolCall),
            "text_search_and_replace" => await TextSearchAndReplace(toolCall),
            "delete" => await Delete(toolCall),
            "move" => await Move(toolCall),
            "compile" => await Compile(toolCall),
            "diff_with_original" => await Diff(toolCall),
            _ => new ToolResult(
                $"Error could not find action '{toolArguments.action}'",
                $"Error could not find action '{toolArguments.action}'",
                true)
        };
    }

    private async Task<ToolResult> Write(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.path == null)
            return new ToolResult(
                "Error parameter path is not supplied",
                "Error parameter path is not supplied",
                true);
        if (toolArguments.content == null)
            return new ToolResult(
                "Error parameter content is not supplied",
                "Error parameter content is not supplied",
                true);

        Workspace.GaurdParseFullPath(toolArguments.path, out var fullPath);

        try
        {
            var file = Workspace.GetFile(toolArguments.path);
            if (file == null)
            {
                var newFile = new WorkspaceFile(toolArguments.path, fullPath);
                await newFile.UpdateContent(toolArguments.content);
                Workspace.Files.Add(newFile);
                return new ToolResult(
                    $"Created {toolArguments.path}:\r\n{toolArguments.content}",
                    $"Created {toolArguments.path}",
                    false);
            }
            else
            {
                var originalFile = Workspace.GetOriginalFile(toolArguments.path);
                var oldContent = originalFile?.Content ?? string.Empty;
                var newContent = await file.GetFileContent() ?? string.Empty;
                var sb = GetDiffText(toolArguments, oldContent, newContent);

                await file.UpdateContent(toolArguments.content);
                return new ToolResult(
                    $"Updated {toolArguments.path} Changes:\r\n{sb}",
                    $"Updated {toolArguments.path}",
                    false);
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
    private async Task<ToolResult> TextSearchAndReplace(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.path == null)
            return new ToolResult(
                "parameter path is not supplied",
                "parameter path is not supplied",
                true);
        if (toolArguments.query == null)
            return new ToolResult(
                "parameter query is not supplied",
                "parameter query is not supplied",
                true);
        if (toolArguments.replaceText == null)
            return new ToolResult(
                "parameter replaceText is not supplied",
                "parameter replaceText is not supplied",
                true);

        var file = Workspace.GetFile(toolArguments.path);
        if (file == null)
            return new ToolResult(
                $"Error could not find path '{toolArguments.path}'",
                $"Error could not find path '{toolArguments.path}'",
                true);

        var content = await file.GetFileContent();
        var fileChanges = Regex.Matches(content, Regex.Escape(toolArguments.query)).Count;
        content = content.Replace(toolArguments.query, toolArguments.replaceText);

        if (fileChanges > 0)
        {
            await file.UpdateContent(content);
        }

        return new ToolResult(
            $"Replaced {fileChanges} instances",
            $"Replaced {fileChanges} instances",
            false);
    }
    private async Task<ToolResult> Delete(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.path == null)
            return new ToolResult(
                "Error parameter path is not supplied",
                "Error parameter path is not supplied",
                true);

        Workspace.GaurdParseFullPath(toolArguments.path, out var fullPath);

        try
        {
            var file = Workspace.GetFile(toolArguments.path);
            if (file != null)
            {
                file.Delete();
                Workspace.Files.Remove(file);
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
    private async Task<ToolResult> Move(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.path == null)
            return new ToolResult(
                "Error parameter path is not supplied",
                "Error parameter path is not supplied",
                true);
        if (toolArguments.newPath == null)
            return new ToolResult(
                "Error parameter newPath is not supplied",
                "Error parameter newPath is not supplied",
                true);

        Workspace.GaurdParseFullPath(toolArguments.newPath, out var newFullPath);

        try
        {
            var file = Workspace.GetFile(toolArguments.path);
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