using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Enums;
using MyCodingAgent.ToolCalls;

namespace MyCodingAgent.Agents;

public class Planner_Agent : BaseAgent, IAgent
{
    public Planner_Agent(IClient client, Workspace workspace, Model model) : base(client, workspace, model)
    {
        WorkspaceTool = new WorkspaceReadonly_Tool(workspace);
        SubTasksTool = new SubTasks_Tool(workspace);
        AskHumanDeveloperTool = new AskHumanDeveloper_Tool(workspace);
        WorkIsAlreadyDoneTool = new WorkIsAlreadyDone_Tool(workspace);

        Tools =
        [
            WorkspaceTool,
            SubTasksTool,
            AskHumanDeveloperTool,
            WorkIsAlreadyDoneTool
        ];
    }

    public string AgentName => "ProjectManagerPlanner_Agent";
    public WorkspaceReadonly_Tool WorkspaceTool { get; }
    public SubTasks_Tool SubTasksTool { get; }
    public AskHumanDeveloper_Tool AskHumanDeveloperTool { get; }
    public WorkIsAlreadyDone_Tool WorkIsAlreadyDoneTool { get; }

    protected override List<ResponseResults> History => Workspace.PlanningHistory;
    protected override IToolCall[] Tools { get; }

    public async Task<ApiCall> GenerateApiCall()
    {
        List<Message> messageList = 
        [
            // SYSTEM PROMPT
            new Message(
                nameof(AgentRole.System).ToLower(),
                null,
                $@"You are a planning agent inside a .NET 10 development workspace.

Your job is to analyze the developer request and create a subtask plan.

You DO NOT modify code.
You ONLY create and manage subtasks.
You can reply multiple tool_calls.

WORKFLOW

1. Understand the developer request
2. Inspect the workspace if needed (use '{WorkspaceTool.Name}' tools)
3. Determine what functionality must be implemented
4. Break the work into clear development subtasks
5. Create subtasks using the '{SubTasksTool.Name}' tool
6. When the full plan is complete call the 'planning_is_done' action of the '{SubTasksTool.Name}' tool

TASK RULES

- SubTasks must be small and implementable
- SubTasks must describe concrete developer work
- SubTasks must be ordered logically
- Prefer 3-10 subtasks per plan

IMPORTANT

- When you have enough information, STOP investigating and start creating subtasks.
- When the plan is complete you MUST call the 'planning_is_done' action of the '{SubTasksTool.Name}' tool.
- The compiler expects a .csproj, .sln or .slnx file in the root of the workspace
- You must target .NET 10 (net10.0) for projects. Do not forget!

If the requested functionality already exists in the codebase you may call {WorkIsAlreadyDoneTool.Name}.",
                null, 
                null),

            // USER ORIGINAL PROMPT
            new Message(
                nameof(AgentRole.User).ToLower(),
                null,
                $@"--- DEVELOPER REQUEST ---
{Workspace.UserPrompt}
--- END OF DEVELOPER REQUEST ---",
                null, 
                null),
        ];

        // CHAT HISTORY
        AddHistoryAndToolCalls(
            messageList, 
            History, 
            [ ..Tools.Select(a => a.ToDto())],
            additionalSizeInBytes: 0);

        return new ApiCall(
            [.. messageList],
            [.. Tools.Select(a => a.ToDto())]);
    }
}