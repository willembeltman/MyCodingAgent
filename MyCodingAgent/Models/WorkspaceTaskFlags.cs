namespace MyCodingAgent.Models;

public class WorkspaceTaskFlags
{
    public bool PlanningIsDoneFlag { get; set; }
    public bool IsDebuggingFlag { get; set; }
    public bool TaskIsDoneFlag { get; set; }
    public bool IsCodeReviewingFlag { get; set; }
}
public class ToolResultFlags
{
    public bool PlanningIsDoneFlag { get; set; }
    public bool DebuggingIsDoneFlag { get; set; }
    public bool TaskIsDoneFlag { get; set; }
    public bool SubTaskIsDoneFlag { get; set; }
}