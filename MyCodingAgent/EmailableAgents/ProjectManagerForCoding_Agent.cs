using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Enums;
using MyCodingAgent.Helpers;
using MyCodingAgent.ToolCalls;
using MyCodingAgent.ToolCalls.AgentCommunication;
using System.Text.Json;

namespace MyCodingAgent.EmailableAgents;

public class ProjectManagerForCoding_Agent : BaseAgent, IEmailableAgent
{
    public ProjectManagerForCoding_Agent(IClient client, Workspace workspace, Model model) : base(client, workspace, model)
    {
        AnswerCoderAgentTool = new CoderNeedsProjectManagerAnswer_Tool(workspace);
        SubTasksTool = new SubTasks_Tool(workspace);
        WorkspaceReadonlyTool = new WorkspaceReadonly_Tool(workspace);

        Tools =
        [
            AnswerCoderAgentTool,
            SubTasksTool,
            WorkspaceReadonlyTool,
        ];
    }

    public AgentType AgentName => AgentType.ProjectManager;
    public AgentType AcceptsFrom_AgentName => AgentType.Coder;
    protected override List<ResponseResults> History => Workspace.PlanningHistory;

    public CoderNeedsProjectManagerAnswer_Tool AnswerCoderAgentTool { get; }
    public SubTasks_Tool SubTasksTool { get; }
    public WorkspaceReadonly_Tool WorkspaceReadonlyTool { get; }
    protected override IToolCall[] Tools { get; }

    public async Task<ApiCall> GenerateApiCall()
    {
        if (Workspace.CodingAgent_To_ProjectManagerAgent_Question == null)
            throw new Exception("No active job found for Project Manager.");

        List<Message> messageList =
        [
            // SYSTEM PROMPT
            new Message(
                nameof(AgentRole.System).ToLower(),
                null,
                $@"You are the Project Manager for a .NET 10 development project. 
Earlier, you created a plan consisting of several subtasks. Now, a Coder Agent is executing one of those tasks and has encountered a blocker or a question.

YOUR MISSION:
1. Analyze the Coder Agent's question in the context of the original project goals and your previous planning.
2. Provide technical clarification, architectural decisions, or missing information.
3. If the question reveals that the original plan was flawed, use '{SubTasksTool.Name}' to refine the plan.
4. Use the '{AnswerCoderAgentTool.Name}' tool to send your definitive answer back to the agent.

CONSTRAINTS:
- You do not write code yourself.
- You provide the guidance so the Coder Agent can continue.
- Use '{WorkspaceReadonlyTool.Name}' tools, if you need to double-check the current state of the code before answering.

RULES:
- You must target .NET 10 (net10.0) for projects. Do not forget!
- Only if it is really unclear you can ask the developer for extra information

When you have the answer, you MUST call '{AnswerCoderAgentTool.Name}'.",
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

        // De vraag van de Coder verpakken we als een specifieke User-message
        var questionContent = $@"### INCOMING CODER AGENT REQUEST
{Workspace.CodingAgent_To_ProjectManagerAgent_Question.Question}

### CONTEXT: CURRENT SUBTASK DEFINITION
{Workspace.GetCurrentSubTask()?.Content}

### GUIDANCE
Please analyze the request above against the subtask definition and provide the necessary information to unblock the Coder.";

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