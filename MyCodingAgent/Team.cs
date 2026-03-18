using MyCodingAgent.Agents;
using MyCodingAgent.Helpers;
using MyCodingAgent.Models;

public class Team
{
    public Team(Workspace workspace, OllamaClient llmService)
    {
        codingAgent = new Coder_Agent(workspace, llmService);
        codingForDebugAgent = new CoderForDebugger_Agent(workspace, llmService);
        debuggerAgent = new Debugger_Agent(workspace, llmService);
        projectManagerPlannerAgent = new ProjectManagerPlanner_Agent(workspace, llmService);
        projectManagerForCodingAgent = new ProjectManagerForCoding_Agent(workspace, llmService);
        projectManagerForDebuggerAgent = new ProjectManagerForDebugger_Agent(workspace, llmService);
        projectManagerCodeReviewerAgent = new ProjectManagerCodeReviewer_Agent(workspace, llmService);
    }

    public ProjectManagerPlanner_Agent projectManagerPlannerAgent { get; }
    public Coder_Agent codingAgent { get; }
    public CoderForDebugger_Agent codingForDebugAgent { get; }
    public Debugger_Agent debuggerAgent { get; }
    public ProjectManagerForCoding_Agent projectManagerForCodingAgent { get; }
    public ProjectManagerForDebugger_Agent projectManagerForDebuggerAgent { get; }
    public ProjectManagerCodeReviewer_Agent projectManagerCodeReviewerAgent { get; }
}