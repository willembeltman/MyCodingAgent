using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Shared.Enums;
using MyCodingAgent.Shared.Helpers;
using MyCodingAgent.Shared.Interfaces;
using MyCodingAgent.Shared.Models;
using MyCodingAgent.ToolCalls;
using MyCodingAgent.ToolCalls.AgentCommunication;
using System.Text.Json;

namespace MyCodingAgent.Agents;

public class CoderForDebugger_Agent : BaseAgent, IAgent
{
    public CoderForDebugger_Agent(ILlmClient client, Workspace workspace, Model model) : base(client, workspace, model)
    {
        WorkspaceTool = new WorkspaceReadonly_Tool(workspace);
        AnswerDebugAgentTool = new DebuggerNeedsCoderAnswer_Tool(workspace);

        Tools =
        [
            WorkspaceTool,
            AnswerDebugAgentTool
        ];
    }

    public WorkspaceReadonly_Tool WorkspaceTool { get; }
    public DebuggerNeedsCoderAnswer_Tool AnswerDebugAgentTool { get; }

    protected override List<PromptResponseResults> History => Workspace.PlanningHistory;
    protected override IToolCall[] Tools { get; }

    public async Task<Prompt> GeneratePrompt()
    {
        if (Workspace.CodingAgent_To_ProjectManagerAgent_Question == null)
            throw new Exception("No active job found for Project Manager.");

        List<Message> messageList =
        [
            // SYSTEM PROMPT
            new Message(
                nameof(AgentRole.System).ToLower(),
                null,
                $@"You are the Coder Agent for a .NET 10 development project. 
Earlier, you've made some changes to the project that broke compilation. Now, a Debug Agent is solving your errors and has encountered a blocker or a question.

YOUR MISSION:
1. Analyze the Debug Agent's question in the context of the original project goals and your previous planning.
2. Provide technical clarification, architectural decisions, or missing information.
3. Use the '{AnswerDebugAgentTool.Name}' tool to send your definitive answer back to the agent.

CONSTRAINTS:
- You do not write code yourself.
- You provide the guidance so the Debug Agent can continue.
- Use '{WorkspaceTool.Name}' tools, if you need to double-check the current state of the code before answering.

When you have the answer, you MUST call '{AnswerDebugAgentTool.Name}'.",
                null,
                null),

            // USER ORIGINAL PROMPT (Het grote doel)
            new Message(
                nameof(AgentRole.User).ToLower(),
                null,
                $"Original Project Goal: {Workspace.UserPrompt}",
                null,
                null),
        ];

        // De vraag van de DebugAgent verpakken we als een specifieke User-message
        var questionContent = $@"### INCOMING DEBUG AGENT REQUEST
{Workspace.CodingAgent_To_ProjectManagerAgent_Question.Question}

### CONTEXT: CURRENT SUBTASK DEFINITION
{Workspace.GetCurrentSubTask()?.Content}

### GUIDANCE
Please analyze the request above against the subtask definition and provide the necessary information to unblock the Debug Agent.";

        var question = new Message(
            nameof(AgentRole.User).ToLower(),
            null,
            questionContent,
            null,
            null);

        var questionJson = JsonSerializer.Serialize(question, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);

        // CHAT HISTORY (Hier zit je create_subtask historie in)
        AddHistoryAndToolCalls(
            messageList,
            History,
            [.. Tools.Select(a => a.ToDto())],
            additionalSizeInBytes: questionJson.Length);

        // Voeg de actuele vraag als laatste toe zodat deze de meeste prioriteit heeft
        messageList.Add(question);

        return new Prompt(
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
    public async Task<bool> ProcessResponse(Prompt prompt, Response agentResponse)
    {
        var response = await GetAgentResponseResult(prompt, agentResponse, Tools);
        History.Add(response);
        return response.ToolCallResults.Any(a => a.result.error == false);
    }
}