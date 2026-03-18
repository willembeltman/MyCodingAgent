using MyCodingAgent.Agents;
using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.ToolCalls;
using MyCodingAgent.ToolCalls.AgentCommunication;

public class Debugger_Agent : BaseAgent, IAgent
{
    public Debugger_Agent(Workspace workspace, OllamaClient client) : base(workspace, client)
    {
        WorkspaceTool = new Workspace_Tool(workspace);
        DebugAgentIsDoneTool = new DebuggingIsDone_Tool(workspace);
        AskCoderAgentTool = new DebuggerNeedsCoder_Tool(workspace);
        //AskProjectManagerTool = new DebugAgent_To_ProjectManager_Question_Tool(workspace);

        Tools =
        [
            WorkspaceTool,
            DebugAgentIsDoneTool,
            AskCoderAgentTool,
            //AskProjectManagerTool
        ];
    }

    public Workspace_Tool WorkspaceTool { get; }
    public DebuggingIsDone_Tool DebugAgentIsDoneTool { get; }
    public DebuggerNeedsCoder_Tool AskCoderAgentTool { get; }
    //public DebugAgent_To_ProjectManager_Question_Tool AskProjectManagerTool { get; }

    protected override List<PromptResponseResults> History => Workspace.DebugHistory;
    protected override IToolCall[] Tools { get; }

    public async Task<OllamaPrompt> GeneratePrompt()
    {
        var compileResult = await Workspace.Compile();
        List <OllamaMessage> messageList =
        [
            // SYSTEM MESSAGE
            new OllamaMessage(
                nameof(OllamaAgentRole.System).ToLower(),
                null,
                $@"You are a .NET 10 repair agent.

GOAL
Fix compilation errors with minimal changes.

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
- Use '{WorkspaceTool.Name}' tool for ALL file operations.
- Target .NET 10 (net10.0) only.",
                null,
                null),
        ];

        var currentSubTaskMessage = new OllamaMessage(
            nameof(OllamaAgentRole.User).ToLower(),
            null,
            $@"--- CURRENT COMPILATION RESULT ---
{compileResult.Content}
--- END OF COMPILATION RESULT ---
Note: this is up-to-date.

GOAL
Make the code compile successfully.
Do not change behavior unless required.

If there are 0 errors in the compilation result, immediately call '{DebugAgentIsDoneTool.Name}' and do not make any changes.",
            null,
            null);
        messageList.Add(currentSubTaskMessage);

        AddHistoryAndToolCalls(
            messageList,
            History,
            [.. Tools.Select(a => a.ToDto())],
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
    public Task<bool> ProcessResponse(OllamaPrompt prompt, OllamaResponse agentResponse)
        => ProcessResponse(prompt, agentResponse, true);
    public async Task<bool> ProcessResponse(OllamaPrompt prompt, OllamaResponse agentResponse, bool save)
    {
        var response = await GetAgentResponseResult(prompt, agentResponse, Tools);
        if (save) History.Add(response);
        return response.ToolCallResults.Any(a => a.result.error == false);
    }
}