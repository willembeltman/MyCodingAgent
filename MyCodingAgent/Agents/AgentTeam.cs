using MyCodingAgent.Models;
using MyCodingAgent.Interfaces;
using MyCodingAgent.EmailableAgents;

namespace MyCodingAgent.Agents;

public class AgentTeam
{
    public Planner_Agent Planner { get; }
    public Coder_Agent Coder { get; }
    public Debugger_Agent Debugger { get; }
    public CodeReviewer_Agent CodeReviewer { get; }

    public CoderForDebugger_Agent CoderForDebugger { get; }
    public ProjectManagerForCoding_Agent ProjectManagerForCoder { get; }
    public ProjectManagerForDebugger_Agent ProjectManagerForDebugger { get; }
    public IEmailableAgent[] EmailableAgents { get; }

    public AgentTeam(IClient client, Workspace workspace, Model model)
    {
        Planner = new Planner_Agent(client, workspace, model);
        Coder = new Coder_Agent(client, workspace, model);
        Debugger = new Debugger_Agent(client, workspace, model);
        CodeReviewer = new CodeReviewer_Agent(client, workspace, model);

        CoderForDebugger = new CoderForDebugger_Agent(client, workspace, model);
        ProjectManagerForCoder = new ProjectManagerForCoding_Agent(client, workspace, model);
        ProjectManagerForDebugger = new ProjectManagerForDebugger_Agent(client, workspace, model);

        EmailableAgents =
        [
            CoderForDebugger,
            ProjectManagerForCoder,
            ProjectManagerForDebugger
        ];

    }
}