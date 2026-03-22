using MyCodingAgent.Enums;
using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.ToolCalls;
using System.Text.Json;

namespace MyCodingAgent.Agents;

public class Debugger_Agent : BaseAgent, IAgent
{
    public Debugger_Agent(IClient client, Workspace workspace, Model model) : base(client, workspace, model)
    {
        WorkspaceTool = new Workspace_Tool(workspace);
        DebugAgentIsDoneTool = new DebuggingIsDone_Tool(workspace);
        AskCoderAgentTool = new AgentToAgent_Question_Tool(workspace, AgentType.Debugger, AgentType.Coder,
            "ask_coder_agent",
            "Ask the coder agent for clarification or missing details",
            "Question or missing information");

        Tools =
        [
            WorkspaceTool,
            DebugAgentIsDoneTool,
            AskCoderAgentTool
        ];
    }

    public AgentType AgentName => AgentType.Debugger;
    public Workspace_Tool WorkspaceTool { get; }
    public DebuggingIsDone_Tool DebugAgentIsDoneTool { get; }
    public AgentToAgent_Question_Tool AskCoderAgentTool { get; }

    protected override List<ResponseResults> History => Workspace.DebugHistory;
    protected override IToolCall[] Tools { get; }

    public async Task<ApiCall> GenerateApiCall()
    {
        var compileResult = await Workspace.Compile();
        var compileResultText = string.Join("\r\n", compileResult.Errors.Take(3).Select(a => a.FullError));

        List <Message> messageList =
        [
            // SYSTEM MESSAGE
            new Message(
                nameof(AgentRole.System).ToLower(),
                null,
                $@"You are a .NET 10 repair agent.

WORKFLOW
1. Analyze the error.
2. Find the root cause.
3. Read relevant files using '{WorkspaceTool.Name}' tool.
4. Apply the smallest possible fix.
5. Repeat until it compiles.
6. Call '{DebugAgentIsDoneTool.Name}' tool when done. DO NOT FORGET!

RULES
- A .csproj, .sln, or .slnx must exist in the ROOT (no sub-directory search).
- Always read a file before modifying it.
- Do not overwrite entire files unless necessary.
- 1 class per file, preferably 1 function per file, refactor if needed.
- Target .NET 10 (net10.0) only.",
                null,
                null),

            new Message(
                nameof(AgentRole.User).ToLower(),
                null,
                $@"GOAL
Fix compilation errors with minimal changes.
Do not change behavior unless required.",
                null,
                null)
        ];

        var currentSubTaskMessage = new Message(
            nameof(AgentRole.User).ToLower(),
            null,
            $@"--- CURRENT COMPILATION RESULT ---
{compileResultText}
--- END OF COMPILATION RESULT ---",
            null,
            null);
        var currentSubTaskMessageJson = JsonSerializer.Serialize(currentSubTaskMessage, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);

        AddHistoryAndToolCalls(
            messageList,
            History,
            [.. Tools.Select(a => a.ToDto())],
            additionalSizeInBytes: currentSubTaskMessageJson.Length);

        messageList.Add(currentSubTaskMessage);

        return new ApiCall(
            [.. messageList],
            [.. Tools.Select(a => a.ToDto())]);
    }
}