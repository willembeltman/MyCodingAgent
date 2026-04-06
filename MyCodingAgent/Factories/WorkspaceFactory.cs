using MyCodingAgent.Extentions;
using MyCodingAgent.Models;
using System.Text.Json;

namespace MyCodingAgent.Factories;

public static class WorkspaceFactory
{
    public static async Task<Workspace?> TryLoad(string rootDirectoryName, CancellationToken ct = default)
    {
        var llmFileString = Path.Combine(rootDirectoryName, "workspace.llm");
        var workspace = (Workspace?)null;
        if (File.Exists(llmFileString))
        {
            using var stream = File.OpenRead(llmFileString);
            workspace = await JsonSerializer.DeserializeAsync<Workspace>(stream);
        }
        return workspace;
    }

    public static async Task<Workspace> Create(string rootDirectoryName)
    {
        var workspace = new Workspace()
        {
            RootDirectoryName = rootDirectoryName
        };
        var rootDirectory = new DirectoryInfo(workspace.RootDirectoryName);
        if (!rootDirectory.Exists)
            rootDirectory.Create();

        // For when the developer has already setup a project and the workspace.llm file is just missing
        workspace.OriginalFiles.Clear();
        await workspace.InitializeDirectory(rootDirectory);

        return workspace;
    }
}

