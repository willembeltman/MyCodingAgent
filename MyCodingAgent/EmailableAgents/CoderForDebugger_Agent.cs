using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Enums;
using MyCodingAgent.Helpers;
using MyCodingAgent.ToolCalls;
using System.Text.Json;

namespace MyCodingAgent.EmailableAgents;

public class CoderForDebugger_Agent : BaseAgent, IEmailableAgent
{
    public CoderForDebugger_Agent(IClient client, Workspace workspace, Model model) : base(client, workspace, model)
    {
        WorkspaceTool = new WorkspaceReadonly_Tool(workspace);
        AnswerDebugAgentTool = new AgentToAgent_Answer_Tool(workspace,
            "answer_debug_question",
            "Provide the official response or missing technical details to a Debug Agent question",
            "The detailed answer or instruction that will be sent back to the coding agent");

        Tools =
        [
            WorkspaceTool,
            AnswerDebugAgentTool
        ];
    }

    public AgentType AgentName => AgentType.Coder;
    public AgentType[] AcceptsFrom_AgentName => [ AgentType.Debugger ];
    protected override List<ResponseResults> History => Workspace.PlanningHistory;

    public WorkspaceReadonly_Tool WorkspaceTool { get; }
    public AgentToAgent_Answer_Tool AnswerDebugAgentTool { get; }
    protected override IToolCall[] Tools { get; }
    private WorkspaceInboxMessage? Message { get; set; }

    public void SetCurrentMessage(WorkspaceInboxMessage? message)
        => AnswerDebugAgentTool.SetCurrentMessage(message);
    public async Task<ApiCall> GenerateApiCall()
    {
        var message = AnswerDebugAgentTool.Message;
        if (Message == null)
            throw new Exception("No active message found for CodingForDebugger_Agent.");

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
{Message.Question}

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

        return new ApiCall(
            [.. messageList],
            [.. Tools.Select(a => a.ToDto())]);
    }

}