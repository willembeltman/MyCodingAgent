using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Enums;
using MyCodingAgent.ToolCalls;

namespace MyCodingAgent.Agents;

public class CodeReviewer_Agent : BaseAgent, IAgent
{
    public CodeReviewer_Agent(Current current) 
        : base(current)
    {
        WorkspaceTool = new WorkspaceReadonly_Tool(current);
        SubTasksTool = new SubTasks_Tool(current);
        CodeReviewIsDoneTool = new CodeReviewIsDone_Tool(current);
        AskHumanDeveloperTool = new AskHumanDeveloper_Question_Tool(current, Actor.CodeReviewer);

        Tools =
        [
            WorkspaceTool,
            SubTasksTool,
            AskHumanDeveloperTool,
            CodeReviewIsDoneTool
        ];
    }

    public Actor AgentName => Actor.CodeReviewer;
    public WorkspaceReadonly_Tool WorkspaceTool { get; }
    public SubTasks_Tool SubTasksTool { get; }
    public CodeReviewIsDone_Tool CodeReviewIsDoneTool { get; }
    public AskHumanDeveloper_Question_Tool AskHumanDeveloperTool { get; }

    protected override IEnumerable<WorkspaceEvent> History 
        => CurrentTask
            .GetEvents(CurrentWorkspace)
            .Where(a => a.Conversation == Conversation.System || a.Conversation == Conversation.CodeReview);
    protected override IToolCall[] Tools { get; }

    public async Task<LlmRequest> GenerateRequest(CompileResult compileResult)
    {
        List<Message> requestMessages = 
        [
            // SYSTEM PROMPT
            new Message(
                nameof(AgentRole.System).ToLower(),
                null,
                $@"You are a .NET 10 code review agent.

GOAL
Review the code according existing subtasks and the overall changes.

WORKFLOW
1. Inspect code using '{WorkspaceTool.Name}'.
2. Review existing subtasks using '{SubTasksTool.Name}'.
3. Consider the full diff and overall architecture.
4. Decide:
   - If work is missing → create new subtasks, then call '{CodeReviewIsDoneTool.Name}'.
   - If everything is complete → call '{CodeReviewIsDoneTool.Name}'.

RULES
- 1 class per file, preferably 1 function per file.

TARGET
- .NET 10 (net10.0) only.",
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


        // CHAT HISTORY
        var requestTools = Tools.Select(a => a.ToDto()).ToArray();
        AddHistoryToMessageList(
            requestMessages,
            requestTools,
            additionalSizeInBytes: 0);

        return new LlmRequest(
            [.. requestMessages],
            requestTools);
    }
}