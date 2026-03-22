using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Enums;
using MyCodingAgent.ToolCalls;

namespace MyCodingAgent.Agents;

public class CodeReviewer_Agent : BaseAgent, IAgent
{
    public CodeReviewer_Agent(IClient client, Workspace workspace, Model model) 
        : base(client, workspace, model)
    {
        WorkspaceTool = new WorkspaceReadonly_Tool(workspace);
        SubTasksTool = new SubTasks_Tool(workspace);
        CodeReviewIsDoneTool = new CodeReviewIsDone_Tool(workspace);
        AskHumanDeveloperTool = new AskHumanDeveloper_Question_Tool(workspace, AgentType.CodeReviewer);

        Tools =
        [
            WorkspaceTool,
            SubTasksTool,
            AskHumanDeveloperTool,
            CodeReviewIsDoneTool
        ];
    }

    public AgentType AgentName => AgentType.CodeReviewer;
    public WorkspaceReadonly_Tool WorkspaceTool { get; }
    public SubTasks_Tool SubTasksTool { get; }
    public CodeReviewIsDone_Tool CodeReviewIsDoneTool { get; }
    public AskHumanDeveloper_Question_Tool AskHumanDeveloperTool { get; }

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