using MyCodingAgent.Models;
using System.Text;

namespace MyCodingAgent.Extentions;

public static class WorkspaceTaskExtensions
{
    public static async Task<string> GetListAllSubTasksText(this WorkspaceTask workspaceTask)
    {
        StringBuilder sb = new StringBuilder();
        if (workspaceTask.SubTasks.Count > 0)
        {
            foreach (var subtask in workspaceTask.SubTasks)
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
    public static WorkspaceSubTask? GetSubTask(this WorkspaceTask workspace, string? id)
        => workspace.SubTasks.FirstOrDefault(a => a.Id.ToString() == id);
    public static WorkspaceSubTask? GetCurrentSubTask(this WorkspaceTask workspace)
        => workspace.SubTasks.FirstOrDefault(a => a.Finished == false);
    public static IEnumerable<WorkspaceEvent> GetEvents(this WorkspaceTask workspaceTask, Workspace workspace) 
        => workspace.Events.Where(a => a.TaskId == workspaceTask.Id);

}
