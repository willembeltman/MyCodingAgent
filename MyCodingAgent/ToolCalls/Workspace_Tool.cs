using MyCodingAgent.Models;
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
            "list",
            "search",
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
        new ("path", "string", "File path, not used for 'list' action", null, true),
        new ("query", "string", "Exact text to find, for 'search', 'text_search' and 'text_search_and_replace' action", null, true),
        new ("content", "string", "Content for 'write', 'append' and 'text_search_and_replace' action", null, true),
        new ("newPath", "string", "Destination path for 'move' action", null, true),
        new ("lineNumber", "number", "Line number for 'append' action (optional)", null, true)
    ];

    public override async Task<ToolResult> Invoke(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Action == null)
            return new ToolResult(
                "Error parameter action is not supplied",
                "Error parameter action is not supplied",
                true);

        return toolArguments.Action.ToLower() switch
        {
            "dir" => await FilesList(toolCall),
            "ls" => await FilesList(toolCall),
            "list" => await FilesList(toolCall),
            "list_files" => await FilesList(toolCall),
            "file_list" => await FilesList(toolCall),
            "files_list" => await FilesList(toolCall),
            "search" => await FilesList(toolCall),
            "open" => await Read(toolCall),
            "read" => await Read(toolCall),
            "create" => await Write(toolCall),
            "write" => await Write(toolCall),
            "append" => await Append(toolCall),
            "delete" => await Delete(toolCall),
            "remove" => await Delete(toolCall),
            "move" => await Move(toolCall),
            "replace" => await TextSearchAndReplace(toolCall),
            "insert" => await Append(toolCall),
            "file_create" => await Write(toolCall),
            "file_open" => await Read(toolCall),
            "file_read" => await Read(toolCall),
            "file_write" => await Write(toolCall),
            "file_append" => await Append(toolCall),
            "file_delete" => await Delete(toolCall),
            "file_remove" => await Delete(toolCall),
            "file_move" => await Move(toolCall),
            "text_search" => await TextSearch(toolCall),
            "text_replace" => await TextSearchAndReplace(toolCall),
            "text_insert" => await Append(toolCall),
            "text_search_and_replace" => await TextSearchAndReplace(toolCall),
            "compile" => await Compile(toolCall),
            "diff" => await Diff(toolCall),
            "diff_with_original" => await Diff(toolCall),
            _ => new ToolResult(
                $"Error could not find action '{toolArguments.Action}'",
                $"Error could not find action '{toolArguments.Action}'",
                true)
        };
    }

    private async Task<ToolResult> Write(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Path == null)
            return new ToolResult(
                "Error parameter path is not supplied",
                "Error parameter path is not supplied",
                true);
        if (toolArguments.Content == null)
            return new ToolResult(
                "Error parameter content is not supplied",
                "Error parameter content is not supplied",
                true);

        Workspace.GaurdParseFullPath(toolArguments.Path, out var fullPath);

        try
        {
            var file = Workspace.GetFile(toolArguments.Path);
            if (file == null)
            {
                var newFile = new WorkspaceFile(toolArguments.Path, fullPath);
                await newFile.UpdateContent(toolArguments.Content);
                Workspace.Files.Add(newFile);
                return new ToolResult(
                    $"Created {toolArguments.Path}:\r\n{toolArguments.Content}",
                    $"Created {toolArguments.Path}",
                    false);
            }
            else
            {
                var originalFile = Workspace.GetOriginalFile(toolArguments.Path);
                var oldContent = originalFile?.Content ?? string.Empty;
                var newContent = await file.GetFileContent() ?? string.Empty;
                var sb = GetDiffText(toolArguments, oldContent, newContent);

                await file.UpdateContent(toolArguments.Content);
                return new ToolResult(
                    $"Updated {toolArguments.Path} Changes:\r\n{sb}",
                    $"Updated {toolArguments.Path}",
                    false);
            }
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error while updating '{toolArguments.Path}': {ex.Message}",
                $"Error while updating",
                true);
        }
    }
    private async Task<ToolResult> Append(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Path == null)
            return new ToolResult(
                "parameter path is not supplied.",
                "parameter path is not supplied.",
                true);
        if (toolArguments.LineNumber == null)
            return new ToolResult(
                "parameter startLine is not supplied.",
                "parameter startLine is not supplied.",
                true);
        if (toolArguments.Content == null)
            return new ToolResult(
                "parameter content is not supplied.",
                "parameter content is not supplied.",
                true);

        var file = Workspace.GetFile(toolArguments.Path);
        if (file == null)
            return new ToolResult(
                $"Error could not find file '{toolArguments.Path}'",
                $"Error could not find file",
                true);

        try
        {
            await file.UpdateContent(
                toolArguments.LineNumber.Value,
                -1,
                toolArguments.Content);
            return new ToolResult(
                $"Appended file '{toolArguments.Path}': \r\n{toolArguments.Content}",
                $"Appended file",
                false);
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error while updating file '{toolArguments.Path}': {ex.Message}",
                $"Error while updating file",
                true);
        }
    }
    private async Task<ToolResult> TextSearchAndReplace(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Path == null)
            return new ToolResult(
                "parameter path is not supplied",
                "parameter path is not supplied",
                true);
        if (toolArguments.Query == null)
            return new ToolResult(
                "parameter query is not supplied",
                "parameter query is not supplied",
                true);
        if (toolArguments.ReplaceText == null)
            return new ToolResult(
                "parameter replaceText is not supplied",
                "parameter replaceText is not supplied",
                true);

        var file = Workspace.GetFile(toolArguments.Path);
        if (file == null)
            return new ToolResult(
                $"Error could not find path '{toolArguments.Path}'",
                $"Error could not find path '{toolArguments.Path}'",
                true);

        var content = await file.GetFileContent();
        var fileChanges = Regex.Count(content, Regex.Escape(toolArguments.Query));
        content = content.Replace(toolArguments.Query, toolArguments.ReplaceText);

        if (fileChanges > 0)
        {
            await file.UpdateContent(content);
        }

        return new ToolResult(
            $"Replaced {fileChanges} instances",
            $"Replaced {fileChanges} instances",
            false);
    }
    private async Task<ToolResult> Delete(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Path == null)
            return new ToolResult(
                "Error parameter path is not supplied",
                "Error parameter path is not supplied",
                true);

        Workspace.GaurdParseFullPath(toolArguments.Path, out var _);

        try
        {
            var file = Workspace.GetFile(toolArguments.Path);
            if (file != null)
            {
                file.Delete();
                Workspace.Files.Remove(file);
                return new ToolResult(
                    $"Deleted file {toolArguments.Path}",
                    $"Deleted file",
                    false);
            }
            return new ToolResult(
                $"Error while deleting file '{toolArguments.Path}': could not find file",
                $"Error while deleting file: could not find",
                true);
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error while deleting file '{toolArguments.Path}': {ex.Message}",
                $"Error while deleting file",
                true);
        }
    }
    private async Task<ToolResult> Move(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Path == null)
            return new ToolResult(
                "Error parameter path is not supplied",
                "Error parameter path is not supplied",
                true);
        if (toolArguments.NewPath == null)
            return new ToolResult(
                "Error parameter newPath is not supplied",
                "Error parameter newPath is not supplied",
                true);

        Workspace.GaurdParseFullPath(toolArguments.NewPath, out var newFullPath);

        try
        {
            var file = Workspace.GetFile(toolArguments.Path);
            if (file != null && file.Exists())
            {
                file.Move(toolArguments.NewPath, newFullPath);
                return new ToolResult(
                    $"Moved file {toolArguments.Path} -> {toolArguments.NewPath}",
                    $"Moved file",
                    false);
            }
            return new ToolResult(
                $"Error while moving file '{toolArguments.Path}': could not find file",
                $"Error while moving file: could not find",
                true);
        }
        catch (Exception ex)
        {
            return new ToolResult(
                $"Error while moving file '{toolArguments.Path}': {ex.Message}",
                $"Error while moving file",
                true);
        }
    }

}