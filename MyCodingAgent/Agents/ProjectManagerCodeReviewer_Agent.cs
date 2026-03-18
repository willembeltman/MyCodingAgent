using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.ToolCalls;

namespace MyCodingAgent.Agents;

public class ProjectManagerCodeReviewer_Agent : BaseAgent, IAgent
{
    public ProjectManagerCodeReviewer_Agent(Workspace workspace, OllamaClient client) : base(workspace, client)
    {
        WorkspaceTool = new WorkspaceReadonly_Tool(workspace);
        SubTasksTool = new SubTasks_Tool(workspace);
        AskHumanDeveloperTool = new Ask_HumanDeveloper_Tool(workspace);
        CodeReviewIsDoneTool = new CodeReviewIsDone_Tool(workspace);

        Tools =
        [
            WorkspaceTool,
            SubTasksTool,
            AskHumanDeveloperTool,
            CodeReviewIsDoneTool
        ];
    }

    public WorkspaceReadonly_Tool WorkspaceTool { get; }
    public SubTasks_Tool SubTasksTool { get; }
    public Ask_HumanDeveloper_Tool AskHumanDeveloperTool { get; }
    public CodeReviewIsDone_Tool CodeReviewIsDoneTool { get; }

    protected override List<PromptResponseResults> History => Workspace.PlanningHistory;
    protected override IToolCall[] Tools { get; }

    public async Task<OllamaPrompt> GeneratePrompt()
    {
        List<OllamaMessage> messageList = 
        [
            // SYSTEM PROMPT
            new OllamaMessage(
                nameof(OllamaAgentRole.System).ToLower(),
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

If the requested functionality already exists in the codebase you may call {CodeReviewIsDoneTool.Name}.",
                null, 
                null),

            // USER ORIGINAL PROMPT
            new OllamaMessage(
                nameof(OllamaAgentRole.User).ToLower(),
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
            maxTokens: 4096,
            additionalSizeInBytes: 0);

        return new OllamaPrompt(
            [.. messageList],
            [.. Tools.Select(a => a.ToDto())]);
    }

    /// <summary>
    /// Processes the agent's response to the specified prompt and determines whether any tool call completed without error.
    /// </summary>
    /// <param name="prompt">The prompt that was sent to the agent. This provides the context or question for which the response is being
    /// processed.</param>
    /// <param name="agentResponse">The response object returned by the agent, containing the results to be evaluated.</param>
    /// <returns>if there was any tool call, if not this indicates maybe a different agent should continue</returns>
    public async Task<bool> ProcessResponse(OllamaPrompt prompt, OllamaResponse agentResponse)
    {
        var response = await GetAgentResponseResult(prompt, agentResponse, Tools);
        History.Add(response);
        return response.ToolCallResults.Any(a => a.result.error == false);
    }
}