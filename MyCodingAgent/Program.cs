using Microsoft.Extensions.Configuration;
using MyCodingAgent.Agents;
using MyCodingAgent.Shared.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.OllamaClient;
using MyCodingAgent.OpenAiClient;
using MyCodingAgent.Shared.Models;

#pragma warning disable CA1859 // Use concrete types when possible for improved performance

internal class Program : IDisposable
{
    readonly CancellationTokenSource Cts;
    readonly IClient Client;

    private Program()
    {
        Console.Clear();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("MyCodingAgent v0.001, created by Willem-Jan Beltman");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Loading appsettings, please wait...");

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();
        var apiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("Cannot find apiKey in appsettings.json");
        }

        Console.WriteLine("Appsettings loaded, loading workspace, please wait...");

        Cts = new CancellationTokenSource();
        Client =
            //new ChatGpt_Client(apiKey);
            new Ollama_Client();
    }

    private async Task StartAsync()
    {
        var workspaceDirectory = Path.Combine(Environment.CurrentDirectory, "Source");
        var workspace = await Workspace.TryLoad(workspaceDirectory);
        if (workspace == null || workspace.Flags.WorkIsDoneFlag)
            workspace = await CreateWorkspace(workspaceDirectory);

        Console.WriteLine("Workspace loaded, getting model list, please wait...");
        var modelList = await Client.GetModels();
        var model = ChooseModel(modelList);

        Console.WriteLine($"Initialising model '{model.Name}', please wait...");
        await Client.InitializeModelAsync(model);

        Console.WriteLine($"Model '{model.Name}' initialized, initialising agents, please wait...");
        var team = new Team(Client, workspace, model);

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
        Model model,
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
            if (NeedsCodeReview(workspace))
                // CODE REVIEW MODE
                await RunCodeReviewLoop(workspace, model, team.projectManagerCodeReviewerAgent);
        }

        await workspace.Save();
    }

    private async Task RunPlanningLoop(Workspace workspace, Model model, ProjectManagerPlanner_Agent planningAgent)
    {
        while (NeedsPlanner(workspace))
        {
            await AgentFlow(workspace, model, planningAgent);
        }
        await workspace.Save();
        Console.Clear();
    }
    private async Task RunCoderNeedsProjectManagerLoop(Workspace workspace, Model model, ProjectManagerForCoding_Agent projectManagerAgent)
    {
        while (CoderNeedsProjectManager(workspace))
        {
            await AgentFlow(workspace, model, projectManagerAgent);
        }
        await workspace.Save();
        Console.Clear();
    }
    private async Task RunDebuggerNeedsProjectManagerLoop(Workspace workspace, Model model, ProjectManagerForDebugger_Agent projectManagerForCodingAgent)
    {
        while (DebuggerNeedsProjectManager(workspace))
        {
            await AgentFlow(workspace, model, projectManagerForCodingAgent);
        }
        await workspace.Save();
        Console.Clear();
    }
    private async Task RunDebuggerNeedsCoderLoop(Workspace workspace, Model model, CoderForDebugger_Agent codingForDebugAgent)
    {
        while (DebuggerNeedsCoder(workspace))
        {
            await AgentFlow(workspace, model, codingForDebugAgent);
        }
        await workspace.Save();
        Console.Clear();
    }
    private async Task RunDebuggerLoop(Workspace workspace, Model model, Debugger_Agent debuggingAgent, CompileResult compileResult)
    {
        while (NeedsDebugging(workspace, compileResult))
        {
            await AgentFlow(workspace, model, debuggingAgent); 
            compileResult = await workspace.Compile();
        }
        await workspace.Save();
        Console.Clear();
    }
    private async Task RunCoderLoop(Workspace workspace, Model model, Coder_Agent codingAgent, CompileResult compileResult)
    {
        while (NeedsCoder(workspace, compileResult))
        {
            await AgentFlow(workspace, model, codingAgent);
            compileResult = await workspace.Compile();
        }
        await workspace.Save();
        Console.Clear();
    }
    private async Task RunCodeReviewLoop(Workspace workspace, Model model, ProjectManagerCodeReviewer_Agent projectManagerCodeReviewerAgent)
    {
        while (NeedsCodeReview(workspace))
        {
            await AgentFlow(workspace, model, projectManagerCodeReviewerAgent);
        }
        await workspace.Save();
        Console.Clear();
    }

    private static bool NeedsPlanner(Workspace workspace)
    {
        return
            workspace.SubTasks.Count == 0 ||
            workspace.Flags.PlanningIsDoneFlag == false;
    }
    private static bool NeedsDebugging(Workspace workspace, CompileResult compileResult)
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
    private static bool CoderNeedsProjectManager(Workspace workspace)
    {
        return
            workspace.CodingAgent_To_ProjectManagerAgent_Question != null;
    }
    private static bool DebuggerNeedsCoder(Workspace workspace)
    {
        return
            workspace.DebugAgent_To_CoderAgent_Question != null;
    }
    private static bool DebuggerNeedsProjectManager(Workspace workspace)
    {
        return
            workspace.DebugAgent_To_ProjectManagerAgent_Question != null;
    }
    private static bool NeedsCoder(Workspace workspace, CompileResult compileResult)
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
    private static bool NeedsCodeReview(Workspace workspace)
    {
        if (workspace.Flags.IsCodeReviewingFlag)
            return true;

        if (workspace.Flags.PlanningIsDoneFlag &&
            workspace.SubTasks.Count != 0 &&
            workspace.SubTasks.Any(a => a.Finished == false) == false)
        {
            workspace.Flags.IsCodeReviewingFlag = true;
            return true;
        }

        return false;
    }

    private async Task AgentFlow(Workspace workspace, Model model, IAgent agent)
    {
        workspace.PromptIndex++;
        var hasToolCalls = false;
        while (!hasToolCalls)
        {
            Console.Clear();
            Console.WriteLine("\x1b[3J");

            var prompt = await agent.GeneratePrompt();
            foreach (var message in prompt.messages)
                ShowMessage(message);
            Console.WriteLine();

            string requestJson = Client.CreateRequestJson(model, prompt);
            Console.WriteLine(requestJson);

            var response = await Client.ChatAsync(model, prompt);
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

    private static async Task<Workspace> CreateWorkspace(string workspaceDirectory)
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
    private static Model ChooseModel(Model[] list)
    {
        var previousColor = Console.ForegroundColor;
        Model? model = null;
        while (model == null)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Choose a model:");
            Console.WriteLine();
            for (var i = 0; i < list.Length; i++)
            {
                Console.WriteLine($"{i}. {list[i].Name} (size: {list[i].MemorySize})");
            }
            Console.WriteLine();
            var numberString = Console.ReadLine();
            if ( int.TryParse(numberString, out var number))
            {
                model = list[number];
            }
        }

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine($"Choosen model: {model.Name}");
        Console.WriteLine();

        Console.ForegroundColor = previousColor;
        return model;
    }
    private static void ShowMessage(Message message)
    {
        //if (!ShownMessages.Add(message)) return;

        var previousColor = Console.ForegroundColor;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{message.Role.ToUpper()}]");
        if (message.Thinking != null)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message.Thinking);
        }
        if (message.ToolCallId != null)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(message.ToolCallId);
            Console.WriteLine(message.Content);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message.Content);
        }
        if (message.ToolCalls != null)
        {
            foreach (var call in message.ToolCalls)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"tool: {call.Function.Name.ToUpper()}");

                if (!string.IsNullOrWhiteSpace( call.Function.Arguments.Action))
                    Console.WriteLine($"action: {call.Function.Arguments.Action.ToUpper()}");

                Console.ForegroundColor = ConsoleColor.Red;
                if (!string.IsNullOrWhiteSpace(call.Function.Arguments.Id))
                    Console.WriteLine($"id: {call.Function.Arguments.Id}");

                if (!string.IsNullOrWhiteSpace(call.Function.Arguments.Path))
                    Console.WriteLine($"path: {call.Function.Arguments.Path}");

                if (!string.IsNullOrWhiteSpace(call.Function.Arguments.Query))
                    Console.WriteLine($"query: {call.Function.Arguments.Query}");

                if (call.Function.Arguments.LineNumber != null)
                    Console.WriteLine($"lineNumber: {call.Function.Arguments.LineNumber}");

                if (!string.IsNullOrWhiteSpace(call.Function.Arguments.NewPath))
                    Console.WriteLine($"newPath: {call.Function.Arguments.NewPath}");

                if (!string.IsNullOrWhiteSpace(call.Function.Arguments.Content))
                    Console.WriteLine($"content: {call.Function.Arguments.Content}");

                if (!string.IsNullOrWhiteSpace(call.Function.Arguments.ReplaceText))
                    Console.WriteLine($"replaceText: {call.Function.Arguments.ReplaceText}");
            }
        }
        Console.WriteLine();

        Console.ForegroundColor = previousColor;
    }

    public void Dispose()
    {
        Cts.Cancel();
        Cts.Dispose();
        Client.Dispose();
    }

    // Main entry point for application
    private static async Task Main()
    {
        using var program = new Program();
        await program.StartAsync();
    }
}