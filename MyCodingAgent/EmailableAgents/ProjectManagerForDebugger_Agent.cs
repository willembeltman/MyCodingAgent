using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Enums;
using MyCodingAgent.Helpers;
using MyCodingAgent.ToolCalls;
using System.Text.Json;

namespace MyCodingAgent.EmailableAgents;

public class ProjectManagerForDebugger_Agent : BaseAgent, IEmailableAgent
{
    public ProjectManagerForDebugger_Agent(IClient client, Workspace workspace, Model model) : base(client, workspace, model)
    {
        AnswerDebugAgentTool = new AgentToAgent_Answer_Tool(workspace,
            "answer_debug_question",
            "Provides the official response or missing technical details to a Coding Agent request",
            "The detailed answer or instruction that will be sent back to the coding agent");
        SubTasksTool = new SubTasks_Tool(workspace);
        WorkspaceTool = new WorkspaceReadonly_Tool(workspace);
        AskHumanDeveloperTool = new AskHumanDeveloper_Question_Tool(workspace, AgentType.ProjectManager);

        Tools =
        [
            AnswerDebugAgentTool,
            SubTasksTool,
            WorkspaceTool,
            AskHumanDeveloperTool
        ];
    }

    public AgentType AgentName => AgentType.ProjectManager;
    public AgentType[] AcceptsFrom_AgentName => [ AgentType.Debugger ];
    protected override List<ResponseResults> History => Workspace.PlanningHistory;

    public AgentToAgent_Answer_Tool AnswerDebugAgentTool { get; }
    public SubTasks_Tool SubTasksTool { get; }
    public WorkspaceReadonly_Tool WorkspaceTool { get; }
    public AskHumanDeveloper_Question_Tool AskHumanDeveloperTool { get; }
    protected override IToolCall[] Tools { get; }
    private WorkspaceInboxMessage? Message { get; set; }

    public void SetCurrentMessage(WorkspaceInboxMessage? message)
        => AnswerDebugAgentTool.SetCurrentMessage(message);
    public async Task<ApiCall> GenerateApiCall()
    {
        var message = AnswerDebugAgentTool.Message;
        if (Message == null)
            throw new Exception("No active job found for Project Manager.");

        List<Message> messageList =
        [
            // SYSTEM PROMPT
            new Message(
                nameof(AgentRole.System).ToLower(),
                null,
                $@"You are the Project Manager for a .NET 10 (net10.0) development project. 
Earlier, you created a plan consisting of several subtasks. Now, a Debug Agent is executing one of those tasks and has encountered a blocker or a question.

YOUR MISSION:
1. Analyze the Debug Agent's question in the context of the original project goals and your previous planning.
2. Provide technical clarification, architectural decisions, or missing information.
3. If the question reveals that the original plan was flawed, use '{SubTasksTool.Name}' to refine the plan.
4. Use the '{AnswerDebugAgentTool.Name}' tool to send your definitive answer back to the agent.

CONSTRAINTS:
- You do not write code yourself.
- You provide the guidance so the Debug can continue.
- Use '{WorkspaceTool.Name}' tools, if you need to double-check the current state of the code before answering.

RULES:
- You must target .NET 10 (net10.0) for projects. Do not forget!
- Only if it is really unclear you can ask the developer for extra information

When you have the answer, you MUST call '{AnswerDebugAgentTool.Name}' tool.",
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

        // De vraag van de Debugg verpakken we als een specifieke User-message
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