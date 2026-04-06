using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace MyCodingAgent.Extentions;

public static class IFileRepositoryExtensions
{
    public static IEnumerable<WorkspaceFile> GetFiles(this IFileRepository repository, Workspace currentWorkspace)
    {
        //var files = repository.OriginalFiles
        //    .Select(a => new WorkspaceFile(a))
        //    .ToList();
        var files = new List<WorkspaceFile>();
        var ioOperations = repository.GetEvents(currentWorkspace)
            .Where(a => a.ToolCallResults != null)
            .SelectMany(a => a.ToolCallResults!)
            .SelectMany(a => a.Result.IoOperations!)
            .ToArray();

        foreach (var ioOperation in ioOperations)
        {
            if (string.IsNullOrWhiteSpace(ioOperation.Path)) continue;
            var path = ioOperation.Path;
            var file = files
                .FirstOrDefault(a =>
                    a.RelativePath.Equals(path.Replace("/", "\\"), StringComparison.CurrentCultureIgnoreCase));
            if (file == null)
            {
                file = new WorkspaceFile(path);
                files.Add(file);
            }
            file.AddIoOperation(ioOperation);
            break;
        }

        return files;
    }

    public static WorkspaceFile? GetFile(this IFileRepository workspaceTask, Workspace workspace, string path)
        => workspaceTask.GetFiles(workspace).FirstOrDefault(a => a.RelativePath.Equals(path.Replace("/", "\\"), StringComparison.CurrentCultureIgnoreCase));



    public static async Task<string> GetListAllFilesText(this IFileRepository workspaceTask, Workspace workspace, string? query)
    {
        StringBuilder sb = new StringBuilder();
        var files = workspaceTask.GetFiles(workspace).ToArray();
        if (!string.IsNullOrWhiteSpace(query))
        {
            sb.AppendLine($"query: '{query}'");
            files = files
                .Where(f => MatchesPattern(f.RelativePath, query))
                .ToArray();
        }
        if (files.Length > 0)
        {
            foreach (var file in files)
            {
                var fileContent = await file.GetContent();
                sb.AppendLine($"{file.RelativePath} ({fileContent.GetLineCount()} lines)");
            }
        }
        else
        {
            sb.AppendLine("<No files found in workspace>");
        }
        return sb.ToString();
    }
    private static bool MatchesPattern(string input, string pattern)
    {
        var regex = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(input, regex, RegexOptions.IgnoreCase);
    }

}
