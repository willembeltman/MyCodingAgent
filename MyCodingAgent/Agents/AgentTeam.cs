using MyCodingAgent.Interfaces;
using MyCodingAgent.EmailableAgents;
using MyCodingAgent.Enums;

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
    public IAgent[] AllAgents { get; }

    public AgentTeam(Current current)
    {
        Planner = new Planner_Agent(current);
        Coder = new Coder_Agent(current);
        Debugger = new Debugger_Agent(current);
        CodeReviewer = new CodeReviewer_Agent(current);

        CoderForDebugger = new CoderForDebugger_Agent(current);
        ProjectManagerForCoder = new ProjectManagerForCoding_Agent(current);
        ProjectManagerForDebugger = new ProjectManagerForDebugger_Agent(current);

        EmailableAgents =
        [
            CoderForDebugger,
            ProjectManagerForCoder,
            ProjectManagerForDebugger
        ];
        AllAgents =
        [
            Planner,
            Coder,
            Debugger,
            CodeReviewer,
            ..EmailableAgents
        ];
    }
}