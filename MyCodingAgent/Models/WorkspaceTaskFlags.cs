namespace MyCodingAgent.Models;

public class WorkspaceTaskFlags : WorkspaceEventFlags
{
    public bool IsDebuggingFlag { get; set; }
    public bool IsCodeReviewingFlag { get; set; }

}
public class WorkspaceEventFlags
{
    public bool PlanningIsDoneFlag { get; set; }
    public bool DebuggingIsDoneFlag { get; set; }
    public bool CodingIsDoneFlag { get; set; }
    public bool TaskIsDoneFlag { get; set; }
}