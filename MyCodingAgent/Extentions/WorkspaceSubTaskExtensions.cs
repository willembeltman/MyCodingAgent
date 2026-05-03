using MyCodingAgent.Models;

namespace MyCodingAgent.Extentions;

public static class WorkspaceSubTaskExtensions
{
    public static IEnumerable<WorkspaceEvent> GetEvents(this WorkspaceSubTask workspaceSubTask, Workspace workspace)
        => workspace.Events.Where(a => a.SubTaskId == workspaceSubTask.Id);
}
