using System.ComponentModel.DataAnnotations;

namespace MyCodingAgent.Models;

public class WorkspaceTask
{
    [Required]
    public int Id { get; init; }
    [Required]
    public string UserPrompt { get; init; } = string.Empty;
    public WorkspaceTaskFlags Flags { get; init; } = new();
    public List<WorkspaceSubTask> SubTasks { get; init; } = [];
}