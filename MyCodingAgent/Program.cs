using MyCodingAgent.Agents;
using MyCodingAgent.Helpers;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

internal class Program : IDisposable
{
    readonly CancellationTokenSource Cts;
    //readonly HashSet<OllamaMessage> ShownMessages;
    readonly OllamaClient LlmService;

    private Program()
    {
        Cts = new CancellationTokenSource();
        //ShownMessages = new HashSet<OllamaMessage>();
        LlmService = new OllamaClient();
    }

    private async Task StartAsync()
    {
        Console.Clear();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("MyCodingAgent v0.001, created by Willem-Jan Beltman");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Loading workspace, please wait...");

        var workspaceDirectory = Path.Combine(Environment.CurrentDirectory, "Source");
        var workspace = await Workspace.TryLoad(workspaceDirectory);
        if (workspace == null || workspace.Flags.WorkIsDoneFlag)
            workspace = await CreateWorkspace(workspaceDirectory);

        Console.WriteLine("Workspace loaded, getting model list, please wait...");
        var modelList = await LlmService.GetModels();
        var model = ChooseModel(modelList);

        Console.WriteLine($"Initialising model '{model.Name}', please wait...");
        await LlmService.InitializeModelAsync(model);

        Console.WriteLine($"Model '{model.Name}' initialized, initialising agents, please wait...");
        var team = new Team(workspace, LlmService);

        //Console.WriteLine("Agents initialized, attempting to compile workspace, please wait...");
        //var compileResult = await workspace.Compile();

        //// Rerun for debug
        //foreach (var resp in workspace.CodingHistory)
        //    await codingAgent.ProcessResponse(resp.Prompt, resp.Response, false);
        //foreach (var resp in workspace.DebugHistory.ToArray())
        //    await debuggingAgent.ProcessResponse(resp.Prompt, resp.Response, false);

        Console.WriteLine("Project compile attempt finished, starting lllm-development-cycle, please wait...");
        await RunMainLoop(workspace, model, team);
    }
    private async Task RunMainLoop(
        Workspace workspace,
        OllamaModel model,
        Team team)
    {
        Console.Clear();

        while (!workspace.Flags.WorkIsDoneFlag)
        {
            if (NeedsPlanner(workspace))
            {
                // PLANNING MODE
                await RunPlanningLoop(workspace, model, team.projectManagerPlannerAgent);
                continue;
            }
            if (CoderNeedsProjectManager(workspace))
            {
                // CODER NEEDS PROJECT MANAGER MODE
                await RunCoderNeedsProjectManagerLoop(workspace, model, team.projectManagerForCodingAgent);
                continue;
            }
            if (DebuggerNeedsProjectManager(workspace))
            {
                // DEBUGGER NEEDS PROJECT MANAGER MODE
                await RunDebuggerNeedsProjectManagerLoop(workspace, model, team.projectManagerForDebuggerAgent);
                continue;
            }
            if (DebuggerNeedsCoder(workspace))
            {
                // DEBUGGER NEEDS CODER MODE
                await RunDebuggerNeedsCoderLoop(workspace, model, team.codingForDebugAgent);
                continue;
            }
            var compileResult = await workspace.Compile();
            if (NeedsDebugging(workspace, compileResult))
            {
                // DEBUGGER MODE
                await RunDebuggerLoop(workspace, model, team.debuggerAgent, compileResult);
                continue;
            }
            if (NeedsCoder(workspace, compileResult))
            {
                // CODER MODE
                await RunCoderLoop(workspace, model, team.codingAgent, compileResult);
                continue;
            }
            if (NeedsCodeReview(workspace, compileResult))
                // CODE REVIEW MODE
                await RunCodeReviewLoop(workspace, model, team.projectManagerCodeReviewerAgent, compileResult);
        }

        await workspace.Save();
    }

    private async Task RunPlanningLoop(Workspace workspace, OllamaModel model, ProjectManagerPlanner_Agent planningAgent)
    {
        while (NeedsPlanner(workspace))
        {
            await AgentFlow(workspace, model, planningAgent);
        }
        await workspace.Save();
        Console.Clear();
    }
    private async Task RunCoderNeedsProjectManagerLoop(Workspace workspace, OllamaModel model, ProjectManagerForCoding_Agent projectManagerAgent)
    {
        while (CoderNeedsProjectManager(workspace))
        {
            await AgentFlow(workspace, model, projectManagerAgent);
        }
        await workspace.Save();
        Console.Clear();
    }
    private async Task RunDebuggerNeedsProjectManagerLoop(Workspace workspace, OllamaModel model, ProjectManagerForDebugger_Agent projectManagerForCodingAgent)
    {
        while (DebuggerNeedsProjectManager(workspace))
        {
            await AgentFlow(workspace, model, projectManagerForCodingAgent);
        }
        await workspace.Save();
        Console.Clear();
    }
    private async Task RunDebuggerNeedsCoderLoop(Workspace workspace, OllamaModel model, CoderForDebugger_Agent codingForDebugAgent)
    {
        while (DebuggerNeedsCoder(workspace))
        {
            await AgentFlow(workspace, model, codingForDebugAgent);
        }
        await workspace.Save();
        Console.Clear();
    }
    private async Task RunDebuggerLoop(Workspace workspace, OllamaModel model, Debugger_Agent debuggingAgent, CompileResult compileResult)
    {
        while (NeedsDebugging(workspace, compileResult))
        {
            await AgentFlow(workspace, model, debuggingAgent); 
            compileResult = await workspace.Compile();
        }
        await workspace.Save();
        Console.Clear();
    }
    private async Task RunCoderLoop(Workspace workspace, OllamaModel model, Coder_Agent codingAgent, CompileResult compileResult)
    {
        while (NeedsCoder(workspace, compileResult))
        {
            await AgentFlow(workspace, model, codingAgent);
            compileResult = await workspace.Compile();
        }
        await workspace.Save();
        Console.Clear();
    }
    private async Task RunCodeReviewLoop(Workspace workspace, OllamaModel model, ProjectManagerCodeReviewer_Agent projectManagerCodeReviewerAgent, CompileResult compileResult)
    {
        while (NeedsCodeReview(workspace, compileResult))
        {
            await AgentFlow(workspace, model, projectManagerCodeReviewerAgent);
        }
        await workspace.Save();
        Console.Clear();
    }

    private bool NeedsPlanner(Workspace workspace)
    {
        return
            workspace.SubTasks.Count == 0 ||
            workspace.Flags.PlanningIsDoneFlag == false;
    }
    private bool NeedsDebugging(Workspace workspace, CompileResult compileResult)
    {
        if (workspace.DebugAgent_To_CoderAgent_Question != null ||
            workspace.DebugAgent_To_ProjectManagerAgent_Question != null)
            return false;

        if (workspace.Flags.IsDebuggingFlag)
            return true;

        var res = compileResult.Errors.Count > 0 &&
               workspace.Files.Count > 0 &&
               workspace.CodingAgent_To_ProjectManagerAgent_Question == null &&
               workspace.DebugAgent_To_ProjectManagerAgent_Question == null;
        if (res)
        {
            workspace.Flags.IsDebuggingFlag = true;
            Console.Clear();
        }
        return res;
    }
    private bool CoderNeedsProjectManager(Workspace workspace)
    {
        return
            workspace.CodingAgent_To_ProjectManagerAgent_Question != null;
    }
    private bool DebuggerNeedsCoder(Workspace workspace)
    {
        return
            workspace.DebugAgent_To_CoderAgent_Question != null;
    }
    private bool DebuggerNeedsProjectManager(Workspace workspace)
    {
        return
            workspace.DebugAgent_To_ProjectManagerAgent_Question != null;
    }
    private bool NeedsCoder(Workspace workspace, CompileResult compileResult)
    {
        return
            workspace.CodingAgent_To_ProjectManagerAgent_Question != null ||
            workspace.DebugAgent_To_CoderAgent_Question != null ||
            workspace.DebugAgent_To_ProjectManagerAgent_Question != null ||
            (
                workspace.GetCurrentSubTask() != null &&
                workspace.Flags.IsDebuggingFlag == false &&
                compileResult.Errors.Count == 0 &&
                workspace.CodingAgent_To_ProjectManagerAgent_Question == null &&
                workspace.DebugAgent_To_ProjectManagerAgent_Question == null
            );
    }
    private bool NeedsCodeReview(Workspace workspace, CompileResult compileResult)
    {
        if (workspace.Flags.IsCodeReviewingFlag)
            return true;

        if (workspace.Flags.PlanningIsDoneFlag &&
            workspace.SubTasks.Any() &&
            workspace.SubTasks.Any(a => a.Finished == false) == false)
        {
            workspace.Flags.IsCodeReviewingFlag = true;
            return true;
        }

        return false;
    }

    private async Task AgentFlow(Workspace workspace, OllamaModel model, IAgent agent)
    {
        workspace.PromptIndex++;
        var hasToolCalls = false;
        while (!hasToolCalls)
        {
            Console.Clear();
            await Task.Delay(250);
            Console.Clear();

            var prompt = await agent.GeneratePrompt();
            foreach (var message in prompt.messages)
                ShowMessage(message);
            Console.WriteLine();

            var response = await LlmService.ChatAsync(model, prompt);
            ShowMessage(response.message);
            Console.WriteLine();

            hasToolCalls = await agent.ProcessResponse(prompt, response);
            await workspace.Save();

            if (workspace.Flags.NeedClearCodingHistoryFlag)
            {
                workspace.CodingHistory.Clear();
                workspace.Flags.NeedClearCodingHistoryFlag = false;
                await workspace.Save();
            }
            if (workspace.Flags.NeedClearDebugHistoryFlag)
            {
                workspace.DebugHistory.Clear();
                workspace.Flags.NeedClearDebugHistoryFlag = false;
                await workspace.Save();
            }
        }
    }


    private async Task<Workspace> CreateWorkspace(string workspaceDirectory)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Please supply a prompt, what do you want to create (use CTRL + enter to submit):");
        string? userPromptText = null;
        var first = true;
        while (userPromptText == null)
        {
            if (first) first = false;
            else
            {
                Console.WriteLine();
                Console.WriteLine("Prompt cannot be empty, please try again:");
            }
            Console.WriteLine();
            userPromptText = ConsoleEditor.ReadMultilineInput();
        }
        var workspace = await Workspace.Create(workspaceDirectory, userPromptText);
        Console.ForegroundColor = previousColor;
        return workspace;
    }
    private OllamaModel ChooseModel(OllamaModel[] list)
    {
        var previousColor = Console.ForegroundColor;
        OllamaModel? model = null;
        while (model == null)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Choose a model:");
            Console.WriteLine();
            for (var i = 0; i < list.Length; i++)
            {
                Console.WriteLine($"{i}. {list[i].Name} (size: {list[i].Size})");
            }
            Console.WriteLine();
            var key = Console.ReadKey();

            if (char.IsDigit(key.KeyChar))
            {
                var choice = key.KeyChar - '0';
                if (choice >= 0 && choice < list.Length)
                {
                    model = list[choice];
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine($"Choosen model: {model.Name}");
        Console.WriteLine();

        Console.ForegroundColor = previousColor;
        return model;
    }
    private void ShowMessage(OllamaMessage message)
    {
        //if (!ShownMessages.Add(message)) return;

        var previousColor = Console.ForegroundColor;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{message.role.ToUpper()}]");
        if (message.thinking != null)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message.thinking);
        }
        if (message.tool_call_id != null)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(message.tool_call_id);
            Console.WriteLine(message.content);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message.content);
        }
        if (message.tool_calls != null)
        {
            foreach (var call in message.tool_calls)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{call.function.name.ToUpper()}");

                if (call.function.arguments.action != null)
                    Console.WriteLine($"action: {call.function.arguments.action.ToUpper()}");

                Console.ForegroundColor = ConsoleColor.Red;
                if (call.function.arguments.id != null)
                    Console.WriteLine($"id: {call.function.arguments.id}");

                if (call.function.arguments.path != null)
                    Console.WriteLine($"path: {call.function.arguments.path}");

                if (call.function.arguments.query != null)
                    Console.WriteLine($"query: {call.function.arguments.query}");

                if (call.function.arguments.lineNumber != null)
                    Console.WriteLine($"lineNumber: {call.function.arguments.lineNumber}");

                if (call.function.arguments.newPath != null)
                    Console.WriteLine($"newPath: {call.function.arguments.newPath}");

                if (call.function.arguments.content != null)
                    Console.WriteLine($"content: {call.function.arguments.content}");

                if (call.function.arguments.replaceText != null)
                    Console.WriteLine($"replaceText: {call.function.arguments.replaceText}");
            }
        }
        Console.WriteLine();

        Console.ForegroundColor = previousColor;
    }

    public void Dispose()
    {
        Cts.Cancel();
        Cts.Dispose();
        LlmService.Dispose();
    }

    // Main entry point for application
    private static async Task Main(string[] args)
    {
        using var program = new Program();
        await program.StartAsync();
    }
}