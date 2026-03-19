using MyCodingAgent.Agents;
using MyCodingAgent.Models;
using MyCodingAgent.Shared.Interfaces;
using MyCodingAgent.Shared.Models;

public class Team
{
    public Team(IClient client, Workspace workspace, Model model)
    {
        codingAgent = new Coder_Agent(client, workspace, model);
        codingForDebugAgent = new CoderForDebugger_Agent(client, workspace, model);
        debuggerAgent = new Debugger_Agent(client, workspace, model);
        projectManagerPlannerAgent = new ProjectManagerPlanner_Agent(client, workspace, model);
        projectManagerForCodingAgent = new ProjectManagerForCoding_Agent(client, workspace, model);
        projectManagerForDebuggerAgent = new ProjectManagerForDebugger_Agent(client, workspace, model);
        projectManagerCodeReviewerAgent = new ProjectManagerCodeReviewer_Agent(client, workspace, model);
    }

    public ProjectManagerPlanner_Agent projectManagerPlannerAgent { get; }
    public Coder_Agent codingAgent { get; }
    public CoderForDebugger_Agent codingForDebugAgent { get; }
    public Debugger_Agent debuggerAgent { get; }
    public ProjectManagerForCoding_Agent projectManagerForCodingAgent { get; }
    public ProjectManagerForDebugger_Agent projectManagerForDebuggerAgent { get; }
    public ProjectManagerCodeReviewer_Agent projectManagerCodeReviewerAgent { get; }
}