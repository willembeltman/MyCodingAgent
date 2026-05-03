using System.ComponentModel.DataAnnotations;

namespace MyCodingAgent.Models;

public class Workspace
{
    [Required]
    public string RootDirectoryName { get; set; } = string.Empty;
    public List<WorkspaceInboxMessage> InboxMessages { get; init; } = [];
    public List<WorkspaceTask> Tasks { get; init; } = [];
    public List<WorkspaceEvent> Events { get; init; } = [];
}