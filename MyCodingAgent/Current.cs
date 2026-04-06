using MyCodingAgent.Extentions;
using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent;

public record Current(
    IClient Client,
    Model Model,
    Workspace Workspace, 
    WorkspaceTask Task)
{
    public WorkspaceSubTask? SubTask => Task.GetCurrentSubTask();
    public bool IsDone => Task.Flags.TaskIsDoneFlag;

    public bool NeedsPlanner()
    {
        return
            Task.SubTasks.Count == 0 ||
            Task.Flags.PlanningIsDoneFlag == false;
    }
    public bool HasInboxMessages()
    {
        return Task.InboxMessages.Count > 0;
    }
    public bool NeedsDebugging(CompileResult compileResult)
    {
        if (Task.Flags.IsDebuggingFlag)
            return true;

        if (compileResult.Errors.Count > 0)
        {
            Task.Flags.IsDebuggingFlag = true;
            return true;
        }
        return false;
    }
    public bool NeedsCoder(CompileResult compileResult)
    {
        return
            Task.GetCurrentSubTask() != null &&
            Task.Flags.IsDebuggingFlag == false &&
            compileResult.Errors.Count == 0;
    }
    public bool NeedsCodeReview()
    {
        if (Task.Flags.IsCodeReviewingFlag)
            return true;

        if (Task.Flags.PlanningIsDoneFlag &&
            Task.SubTasks.Count != 0 &&
            Task.SubTasks.Any(a => a.Finished == false) == false)
        {
            Task.Flags.IsCodeReviewingFlag = true;
            return true;
        }

        return false;
    }
    public void GaurdParseFullPath(string path, out string fullPath)
    {
        var currentDirectory = new DirectoryInfo(Workspace.RootDirectoryName);

        fullPath = Path.GetFullPath(
            Path.Combine(currentDirectory.FullName, path));

        if (!fullPath.StartsWith(currentDirectory.FullName + Path.DirectorySeparatorChar))
            throw new Exception($"LLM path escape detected: {path}");
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
            var currentDirectory = new DirectoryInfo(Workspace.RootDirectoryName);
            var compileResult = await Compiler.Compile(currentDirectory);
            return compileResult;
        }
    }
}
