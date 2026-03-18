using MyCodingAgent.Helpers;
using MyCodingAgent.ToolCalls.AgentCommunication.Models;
using System.Text;
using System.Text.Json;

namespace MyCodingAgent.Models;

public class Workspace
{
    public int PromptIndex { get; set; }
    public string RootDirectoryName { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public int? CurrentSubTask { get; set; }
    public WorkspaceFlags Flags { get; set; } = new();
    public List<WorkspaceOriginalFile> OriginalFiles { get; set; } = [];
    public List<WorkspaceFile> Files { get; set; } = [];
    public List<WorkspaceSubTask> SubTasks { get; set; } = [];
    public List<PromptResponseResults> PlanningHistory { get; set; } = [];
    public List<PromptResponseResults> CodingHistory { get; set; } = [];
    public List<PromptResponseResults> DebugHistory { get; set; } = [];
    public CoderNeedsProjectManager_Question? CodingAgent_To_ProjectManagerAgent_Question { get; set; }
    public DebuggerNeedsProjectManager_Question? DebugAgent_To_ProjectManagerAgent_Question { get; set; }
    public DebuggerNeedsCoder_Question? DebugAgent_To_CoderAgent_Question { get; set; }

    public WorkspaceFile? GetFile(string path)
        => Files.FirstOrDefault(a => a.RelativePath.Equals(path.Replace("/", "\\"), StringComparison.CurrentCultureIgnoreCase));
    public WorkspaceOriginalFile? GetOriginalFile(string path)
        => OriginalFiles.FirstOrDefault(a => a.RelativePath.Equals(path.Replace("/", "\\"), StringComparison.CurrentCultureIgnoreCase));
    public WorkspaceSubTask? GetSubTask(string? id)
        => SubTasks.FirstOrDefault(a => a.Id.ToString() == id);
    public WorkspaceSubTask? GetCurrentSubTask()
    {
        if (CurrentSubTask == null && SubTasks.Count > 0)
        {
            var subtask = SubTasks.First(a => a.Finished == false);
            CurrentSubTask = subtask.Id;
            return subtask;
        }
        if (CurrentSubTask == null)
            return null;
        var current = GetSubTask(CurrentSubTask.Value.ToString());
        if (current?.Finished == true)
        {
            current = SubTasks.FirstOrDefault(a => a.Finished == false);
            CurrentSubTask = current?.Id;
        }
        return current;
    }

    public async static Task<Workspace?> TryLoad(string rootDirectoryName, CancellationToken ct = default)
    {
        var llmFileString = Path.Combine(rootDirectoryName, "workspace.llm");
        var workspace = (Workspace?)null;
        if (File.Exists(llmFileString))
        {
            using (var stream = File.OpenRead(llmFileString))
            {
                workspace = JsonSerializer.Deserialize<Workspace>(stream);
            }
            if (workspace != null)
            {
                await InitializeWorkspace(workspace);
            }
        }
        return workspace;
    }
    public async static Task<Workspace> Create(string rootDirectoryName, string userPrompt)
    {
        var workspace = new Workspace()
        {
            RootDirectoryName = rootDirectoryName,
            UserPrompt = userPrompt,
        };
        await InitializeWorkspace(workspace);
        return workspace;
    }
    private static async Task InitializeWorkspace(Workspace workspace)
    {
        var rootDirectory = new DirectoryInfo(workspace.RootDirectoryName);
        if (!rootDirectory.Exists)
            rootDirectory.Create();

        // For when the developer has already setup a project and the workspace.llm file is just missing
        workspace.Files.Clear();
        workspace.OriginalFiles.Clear();
        await workspace.InitializeDirectory(rootDirectory);
        await workspace.Save();
    }

    private async Task InitializeDirectory(DirectoryInfo directoryInfo, bool isRoot = true, CancellationToken ct = default)
    {
        foreach (var dir in directoryInfo.GetDirectories())
        {
            if (isRoot && dir.Name == "obj") continue;
            if (isRoot && dir.Name == "bin") continue;
            if (isRoot && dir.Name == ".vs") continue;
            await InitializeDirectory(dir, false, ct);
        }

        foreach (var file in directoryInfo.GetFiles())
        {
            if (isRoot && file.Name == "workspace.llm") continue;
            if (file.Extension is ".dll" or ".exe" or ".png" or ".jpg" or ".zip")
                continue;
            var relativePath = Path.GetRelativePath(RootDirectoryName, file.FullName);
            var fullPath = file.FullName;
            var content = await File.ReadAllTextAsync(fullPath);
            var workspaceFile = new WorkspaceFile(relativePath, fullPath);
            Files.Add(workspaceFile);
            var workspaceOriginalFile = new WorkspaceOriginalFile(relativePath, fullPath, content);
            OriginalFiles.Add(workspaceOriginalFile);
        }
    }

    public void GaurdParseFullPath(string path, out string fullPath)
    {
        var currentDirectory = new DirectoryInfo(RootDirectoryName);

        fullPath = Path.GetFullPath(
            Path.Combine(currentDirectory.FullName, path));

        if (!fullPath.StartsWith(currentDirectory.FullName + Path.DirectorySeparatorChar))
            throw new Exception($"LLM path escape detected: {path}");
    }
    public async Task Save()
    {
        var llmFileString = Path.Combine(RootDirectoryName, "workspace.llm");
        if (File.Exists(llmFileString))
            File.Delete(llmFileString);

        using var stream = File.OpenWrite(llmFileString);
        await JsonSerializer.SerializeAsync(stream, this);
    }
    public async Task<CompileResult> Compile(string? relativePath = null)
    {
        if (!string.IsNullOrWhiteSpace(relativePath) &&
            relativePath.ToLower().EndsWith(".csproj") &&
            relativePath.ToLower().EndsWith(".sln") &&
            relativePath.ToLower().EndsWith(".slnx"))
        {
            GaurdParseFullPath(relativePath, out var fullPath);
            var solutionOrProjectFile = new FileInfo(fullPath);
            if (solutionOrProjectFile == null)
            {
                return new CompileResult()
                {
                    Content = $"No .sln, .slnx or .csproj file was found on '{relativePath}'.",
                    Errors = [
                        new CompileError(
                        $"No .sln, .slnx or .csproj file was found on '{relativePath}'."
                        )
                    ]
                };
            }
            var compileResult = await Compiler.Compile(solutionOrProjectFile);
            return compileResult;
        }
        else
        {
            var currentDirectory = new DirectoryInfo(RootDirectoryName);
            var compileResult = await Compiler.Compile(currentDirectory);
            return compileResult;
        }
    }
    public async Task<string> GetListAllFilesText()
    {
        StringBuilder sb = new StringBuilder();
        if (Files.Count > 0)
        {
            foreach (var file in Files)
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
    public async Task<string> GetListAllSubTasksText()
    {
        StringBuilder sb = new StringBuilder();
        if (SubTasks.Count > 0)
        {
            foreach (var subtask in SubTasks)
            {
                var subtaskContent = subtask.Content;
                sb.AppendLine($"# {subtask.Id}");
                sb.AppendLine($"{subtask.Content}");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("<No subtasks found in current project>");
        }
        return sb.ToString();
    }
}