using MyCodingAgent.Agents;
using MyCodingAgent.Enums;
using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.ToolCalls;
using System.Text.Json;

namespace MyCodingAgent.EmailableAgents;

public class ProjectManagerForDebugger_Agent : Planner_Agent, IEmailableAgent
{
    public ProjectManagerForDebugger_Agent(Current current) : base(current)
    {
        AnswerDebugAgentTool = new AgentToAgent_Answer_Tool(current,
            "answer_debug_question",
            "Provides the official response or missing technical details to a Coding Agent request",
            "The detailed answer or instruction that will be sent back to the coding agent");
        SubTasksTool = new SubTasks_Tool(current);
        WorkspaceTool = new WorkspaceReadonly_Tool(current);
        AskHumanDeveloperTool = new AskHumanDeveloper_Question_Tool(current, Actor.ProjectManager);

        Tools =
        [
            AnswerDebugAgentTool,
            SubTasksTool,
            WorkspaceTool,
            AskHumanDeveloperTool
        ];
    }

    private AgentToAgent_Answer_Tool AnswerDebugAgentTool { get; }
    private SubTasks_Tool SubTasksTool { get; }
    private WorkspaceReadonly_Tool WorkspaceTool { get; }
    private AskHumanDeveloper_Question_Tool AskHumanDeveloperTool { get; }

    public override Actor AgentName => Actor.ProjectManager;
    public Actor[] AcceptsFrom_AgentName => [ Actor.Debugger ];
    protected override IToolCall[] Tools { get; }
    private WorkspaceInboxMessage? Message { get; set; }

    public void SetCurrentMessage(WorkspaceInboxMessage? message)
        => AnswerDebugAgentTool.SetCurrentMessage(message);
    public override async Task<LlmRequest> GenerateRequest(CompileResult compileResult)
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
                $"Original Project Goal: {CurrentTask.UserPrompt}",
                null,
                null),
        ];

        // De vraag van de Debugg verpakken we als een specifieke User-message
        var questionContent = $@"### INCOMING DEBUG AGENT REQUEST
{Message.Question}

### CONTEXT: CURRENT SUBTASK DEFINITION
{CurrentSubTask?.Content}

### GUIDANCE
Please analyze the request above against the subtask definition and provide the necessary information to unblock the Debug Agent.";

        var question = new Message(
            nameof(AgentRole.User).ToLower(),
            null,
            questionContent,
            null,
            null);

        var questionJson = JsonSerializer.Serialize(question, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
        var tools = Tools.Select(a => a.ToDto()).ToArray();

        // CHAT HISTORY (Hier zit je create_subtask historie in)
        AddHistoryToMessageList(
            messageList,
            tools,
            additionalSizeInBytes: questionJson.Length);

        // Voeg de actuele vraag als laatste toe zodat deze de meeste prioriteit heeft
        messageList.Add(question);

        return new LlmRequest(
            [.. messageList],
            tools);
    }
}