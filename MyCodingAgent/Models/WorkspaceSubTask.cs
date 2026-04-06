using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MyCodingAgent.Models;

public class WorkspaceSubTask : IFileRepository
{
    [Required]
    public long Id { get; init; }
    [Required]
    public long WorkspaceTaskId { get; init; }
    [Required]
    public string Content { get; set; } = string.Empty;
    //[Required]
    //public List<WorkspaceOriginalFile> OriginalFiles { get; init; } = []; // Snapshot van orginele staat voor task (dus wel met changes van vorige tasks), voor sneller reconstruatie
    public bool Finished { get; set; }

    public IEnumerable<WorkspaceEvent> GetEvents(Workspace workspace) => workspace.Events.Where(a => a.SubTaskId == Id);
    public string GetRootDirectoryName(Workspace workspace) => workspace.RootDirectoryName;
}
