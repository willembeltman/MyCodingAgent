using MyCodingAgent.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MyCodingAgent.Models;

public class Workspace : IFileRepository
{
    [Required]
    public string RootDirectoryName { get; set; } = string.Empty;


    //public List<WorkspaceOriginalFile> OriginalFiles { get; init; } = []; // Allereerste originele staat van de workspace (voor gebruik agent)
    public List<WorkspaceEvent> Events { get; init; } = [];
    public List<WorkspaceTask> Tasks { get; init; } = [];

    public string GetRootDirectoryName(Workspace workspace) => RootDirectoryName;
    public IEnumerable<WorkspaceEvent> GetEvents(Workspace workspace) => workspace.Events;
}