using MyCodingAgent.Helpers;
using MyCodingAgent.Models;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MyCodingAgent.Extentions;

public static class WorkspaceExtensions
{
    public static async Task<WorkspaceTask> CreateTask(this Workspace workspace, string userPrompt)
    {
        var workspaceTask = new WorkspaceTask()
        {
            UserPrompt = userPrompt,
            //OriginalFiles = [.. workspace
            //    .GetFiles(workspace)
            //    .Select(a =>
            //        new WorkspaceOriginalFile(
            //            a.RelativePath,
            //            a.GetContent()))]
        };
        await workspace.Save();

        return workspaceTask;
    }
    public static async Task CheckWorkspace(this Workspace workspace, string baseDirecoryName)
    {

    }

    public static async Task InitializeDirectory(this Workspace workspace, DirectoryInfo directoryInfo, bool isRoot = true, CancellationToken ct = default)
    {
        foreach (var dir in directoryInfo.GetDirectories())
        {
            if (isRoot && dir.Name == "obj") continue;
            if (isRoot && dir.Name == "bin") continue;
            if (isRoot && dir.Name == ".vs") continue;
            await InitializeDirectory(workspace, dir, false, ct);
        }

        foreach (var file in directoryInfo.GetFiles())
        {
            if (isRoot && file.Name == "workspace.llm") continue;
            if (file.Extension is ".dll" or ".exe" or ".png" or ".jpg" or ".zip")
                continue;
            var relativePath = Path.GetRelativePath(workspace.RootDirectoryName, file.FullName);
            var fullPath = file.FullName;
            var content = await File.ReadAllTextAsync(fullPath);
            //var workspaceOriginalFile = new WorkspaceOriginalFile(relativePath, content);
            //workspace.OriginalFiles.Add(workspaceOriginalFile);
        }
    }
    public static IEnumerable<WorkspaceFile> GetFiles(this Workspace currentWorkspace, long? eventId = null)
    {
        var files = new List<WorkspaceFile>();
        var ioOperations = currentWorkspace.Events
            .SelectMany(a => a.Result != null ? a.Result.Events : [])
            .SelectMany(a => a.result.IoOperations)
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

    public static WorkspaceFile? GetFile(this Workspace workspace, string path, long? eventId = null)
    {
        return GetFiles(workspace, eventId).FirstOrDefault(a => a.RelativePath.Equals(path.Replace("/", "\\"), StringComparison.CurrentCultureIgnoreCase));
    }

    public static async Task<string> GetListAllFilesText(this Workspace workspace, string? query)
    {
        StringBuilder sb = new StringBuilder();
        var files = workspace.GetFiles().ToArray();
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
                var fileContent = await file.GetFileContent();
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

    //public static IEnumerable<WorkspaceFile> GetFiles(this Workspace workspace)
    //{
    //    var files = workspace.OriginalFiles
    //        .Select(a => new WorkspaceFile(a))
    //        .ToList();
    //    var ioOperations = workspace.History
    //        .Where(a => a.ResponseResults != null)
    //        .SelectMany(a => a.ResponseResults!.ToolCallResults)
    //        .Where(a => a.result.IoOperation != null)
    //        .Select(a => a.result.IoOperation!)
    //        .ToArray();

    //    foreach (var ioOperation in ioOperations)
    //    {
    //        if (string.IsNullOrWhiteSpace(ioOperation.Path)) continue;
    //        var path = ioOperation.Path;
    //        var file = files
    //            .FirstOrDefault(a =>
    //                a.RelativePath.Equals(path.Replace("/", "\\"), StringComparison.CurrentCultureIgnoreCase));
    //        if (file == null)
    //        {
    //            file = new WorkspaceFile(path);
    //            files.Add(file);
    //        }
    //        file.AddIoOperation(ioOperation);
    //        break;
    //    }

    //    return files;
    //}

    //public static WorkspaceFile? GetFile(this Workspace workspace, string path)
    //    => workspace
    //        .GetFiles(workspace)
    //            .FirstOrDefault(a =>
    //                a.RelativePath.Equals(path.Replace("/", "\\"), StringComparison.CurrentCultureIgnoreCase));

    public static WorkspaceTask? GetCurrentTask(this Workspace workspace)
        => workspace.Tasks.FirstOrDefault(a => a.Flags.TaskIsDoneFlag == false);

    public static int GetNewEventId(this Workspace workspace)
    {
        return workspace.Events.Max(a => a.Id) + 1;
    }
    public static bool HasInboxMessages(this Workspace workspace)
    {
        return workspace.InboxMessages.Count > 0;
    }


    public static async Task Save(this Workspace workspace)
    {
        var llmFileString = Path.Combine(workspace.RootDirectoryName, "workspace.llm");
        if (File.Exists(llmFileString))
            File.Delete(llmFileString);

        using var stream = File.OpenWrite(llmFileString);
        await JsonSerializer.SerializeAsync(stream, workspace);
    }
}
