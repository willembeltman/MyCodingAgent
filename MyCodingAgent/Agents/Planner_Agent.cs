using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Enums;
using MyCodingAgent.ToolCalls;

namespace MyCodingAgent.Agents;

public class Planner_Agent : BaseAgent, IAgent
{
    public Planner_Agent(Current current) : base(current)
    {
        WorkspaceTool = new WorkspaceReadonly_Tool(current);
        SubTasksTool = new SubTasks_Tool(current);
        WorkIsAlreadyDoneTool = new WorkIsAlreadyDone_Tool(current);
        AskHumanDeveloperTool = new AskHumanDeveloper_Question_Tool(current, Actor.Planner);

        Tools =
        [
            WorkspaceTool,
            SubTasksTool,
            WorkIsAlreadyDoneTool,
            AskHumanDeveloperTool,
        ];
    }

    private WorkspaceReadonly_Tool WorkspaceTool { get; }
    private SubTasks_Tool SubTasksTool { get; }
    private AskHumanDeveloper_Question_Tool AskHumanDeveloperTool { get; }
    private WorkIsAlreadyDone_Tool WorkIsAlreadyDoneTool { get; }

    public virtual Actor AgentName => Actor.Planner;
    protected override IEnumerable<WorkspaceEvent> History
        => CurrentTask
            .GetEvents(CurrentWorkspace)
            .Where(a => a.Conversation == Conversation.System || a.Conversation == Conversation.Planning);
    protected override IToolCall[] Tools { get; }

    public virtual async Task<LlmRequest> GenerateRequest(CompileResult result)
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
{CurrentTask.UserPrompt}
--- END OF DEVELOPER REQUEST ---",
                null, 
                null),
        ];
        var tools = Tools.Select(a => a.ToDto()).ToArray();
        // CHAT HISTORY
        AddHistoryToMessageList(
            messageList, 
            tools,
            additionalSizeInBytes: 0);

        return new LlmRequest(
            [.. messageList],
            tools);
    }
}