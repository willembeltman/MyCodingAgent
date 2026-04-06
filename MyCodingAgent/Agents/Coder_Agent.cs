using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Enums;
using MyCodingAgent.ToolCalls;
using MyCodingAgent.Extentions;

namespace MyCodingAgent.Agents;

public class Coder_Agent : BaseAgent, IAgent
{
    public Coder_Agent(Current current) : base(current)
    {
        WorkspaceTool = new Workspace_Tool(current);
        AskProjectManagerTool = new AgentToAgent_Question_Tool(current, Actor.Coder, Actor.ProjectManager, 
            "ask_project_manager_agent",
            "Use when blocked by missing info or unclear requirements",
            "The specific question or missing information needed to proceed with the task");
        CurrentSubTaskIsFinishedTool = new SubTaskIsFinished_Tool(current);

        Tools =
        [
            WorkspaceTool,
            AskProjectManagerTool,
            CurrentSubTaskIsFinishedTool
        ];
    }

    private Workspace_Tool WorkspaceTool { get; }
    private AgentToAgent_Question_Tool AskProjectManagerTool { get; }
    private SubTaskIsFinished_Tool CurrentSubTaskIsFinishedTool { get; }

    public Actor AgentName => Actor.Coder;
    protected override IEnumerable<WorkspaceEvent> History
        => CurrentSubTask?
            .GetEvents(CurrentWorkspace)
            .Where(a => a.Conversation == Conversation.System || a.Conversation == Conversation.Coding)
            ?? [];
    protected override IToolCall[] Tools { get; }

    public virtual async Task<LlmRequest> GenerateRequest(CompileResult compileResult)
    {
        List<Message> messageList =
        [
            // SYSTEM PROMPT
            new Message(
                nameof(AgentRole.System).ToLower(),
                null,
                $@"You are an autonomous software engineering agent operating inside a .NET 10 development workspace. 
You have been assigned a subtask for the project in your workspace.
You must complete this subtask by applying all required changes.

WORKFLOW

1. Understand the request.
2. Inspect files before changing them.
3. Make the smallest possible change to achieve the goal. Do not rewrite entire files unless absolutely necessary.
4. Ask project manager for advice using '{AskProjectManagerTool.Name}' tool_call, if you are unsure.
5. Compile the project using the '{WorkspaceTool.Name}' tool_call.
6. Fix any compilation warnings if they occur.
7. Verify your edits using 'diff_with_original' action of the '{WorkspaceTool.Name}' tool_call.
8. If everything is correct, call the '{CurrentSubTaskIsFinishedTool.Name}' tool_call.

RULES

- The compiler expects a .csproj, .sln or .slnx file in the root of the workspace.
- 1 class per file, preferably 1 function per file, refactor if needed.
- When the code compiles successfully and the requested functionality is implemented,
  you can call the '{CurrentSubTaskIsFinishedTool.Name}' tool_call.
- You must target .NET 10 (net10.0) for projects.",
                null,
                null)
        ];

        var currentSubTask = CurrentTask.GetCurrentSubTask();
        if (currentSubTask != null)
        {
            var currentSubTaskMessage = new Message(
                nameof(AgentRole.User).ToLower(),
                null,
                $@"--- CURRENT SUBTASK ---
{currentSubTask.Content}
--- END OF SUBTASK ---",
                null,
                null);
            messageList.Add(currentSubTaskMessage);
        }

        // CHAT HISTORY
        var requestTools = Tools.Select(a => a.ToDto()).ToArray();
        AddHistoryToMessageList(
            messageList,
            requestTools,
            additionalSizeInBytes: 0);

        return new LlmRequest(
            [.. messageList],
            requestTools);
    }

}