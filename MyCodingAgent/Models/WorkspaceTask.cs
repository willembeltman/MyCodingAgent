using MyCodingAgent.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MyCodingAgent.Models;

public class WorkspaceTask : IFileRepository
{
    [Required]
    public int Id { get; init; }
    [Required]
    public string UserPrompt { get; init; } = string.Empty;
    public WorkspaceTaskFlags Flags { get; init; } = new();
    public List<WorkspaceSubTask> SubTasks { get; init; } = [];
    public List<WorkspaceInboxMessage> InboxMessages { get; set; } = [];
    //public List<WorkspaceOriginalFile> OriginalFiles { get; init; } = []; // Snapshot van orginele staat voor task (dus wel met changes van vorige tasks), voor sneller reconstruatie

    public string GetRootDirectoryName(Workspace workspace) => workspace.RootDirectoryName; // Voor in de toekomst een andere map of zo
    public IEnumerable<WorkspaceEvent> GetEvents(Workspace workspace) => workspace.Events.Where(a => a.TaskId == Id);
}