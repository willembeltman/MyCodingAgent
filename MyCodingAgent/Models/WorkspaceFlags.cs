namespace MyCodingAgent.Models;

public class WorkspaceFlags
{
    public bool PlanningIsDoneFlag { get; set; }
    public bool IsDebuggingFlag { get; set; }
    public bool IsCodeReviewingFlag { get; set; }
    public bool NeedClearCodingHistoryFlag { get; set; }
    public bool NeedClearDebugHistoryFlag { get; set; }
    public bool WorkIsDoneFlag { get; set; }
}
