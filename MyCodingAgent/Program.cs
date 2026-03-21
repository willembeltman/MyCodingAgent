using Microsoft.Extensions.Configuration;
using MyCodingAgent.Agents;
using MyCodingAgent.Enums;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.OllamaClient;

#pragma warning disable CA1859 // Use concrete types when possible for improved performance

internal class Program : IDisposable
{
    readonly CancellationTokenSource Cts;
    readonly IClient Client;
    readonly Dictionary<(AgentType from, AgentType to), Func<AgentTeam, IEmailableAgent>> EmailableAgents = new()
    {
        { (AgentType.Coder, AgentType.ProjectManager), team => team.ProjectManagerForCoder },
        { (AgentType.Debugger, AgentType.ProjectManager), team => team.ProjectManagerForDebugger },
        { (AgentType.Debugger, AgentType.Coder), team => team.CoderForDebugger },
    };

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
        var team = new AgentTeam(Client, workspace, model);

        Console.WriteLine("Project compile attempt finished, starting lllm-development-cycle, please wait...");
        await RunMainLoop(workspace, model, team);
    }
    private async Task RunMainLoop(Workspace workspace, Model model, AgentTeam team)
    {
        Console.Clear();

        while (!workspace.Flags.WorkIsDoneFlag)
        {
            if (workspace.NeedsPlanner())
            {
                // PLANNING MODE
                await RunPlanningLoop(workspace, model, team);
                continue;
            }
            var compileResult = await workspace.Compile();
            if (workspace.HasInboxMessages())
            {
                // MESSAGE BETWEEN AGENTS
                await RunInboxLoop(workspace, model, team, compileResult);
                continue;
            }
            if (workspace.NeedsDebugging(compileResult))
            {
                // DEBUGGER MODE
                await RunDebuggerLoop(workspace, model, team, compileResult);
                continue;
            }
            if (workspace.NeedsCoder(compileResult))
            {
                // CODER MODE
                await RunCoderLoop(workspace, model, team, compileResult);
                continue;
            }
            if (workspace.NeedsCodeReview())
            {
                // CODE REVIEW MODE
                await RunCodeReviewLoop(workspace, model, team);
            }
        }

        await workspace.Save();
    }

    private async Task RunPlanningLoop(Workspace workspace, Model model, AgentTeam team)
    {
        while (workspace.NeedsPlanner())
        {
            await AgentFlow(workspace, model, team.Planner);
        }
        await workspace.Save();
    }
    private async Task RunInboxLoop(Workspace workspace, Model model, AgentTeam team, CompileResult compileResult)
    {
        var message = workspace.InboxMessages.LastOrDefault() ??
            throw new Exception("Er gaat iets mis in de flow, waarom wordt deze functie aangeroepen als er geen messages in de inbox staan.");
        if (!EmailableAgents.TryGetValue((message.From, message.To), out var emailableAgentGetter))
            throw new Exception("Er gaat iets mis in de flow, waarom wordt deze functie aangeroepen met een niet bekende from/to.");
        var emailableAgent = emailableAgentGetter(team);
        emailableAgent.SetCurrentMessage(message);
        while (workspace.InboxMessages.LastOrDefault() == message)
        {
            await AgentFlow(workspace, model, emailableAgent);
        }
    }
    private async Task RunDebuggerLoop(Workspace workspace, Model model, AgentTeam team, CompileResult compileResult)
    {
        while (workspace.NeedsDebugging(compileResult))
        {
            await AgentFlow(workspace, model, team.Debugger);
            compileResult = await workspace.Compile();
        }
    }
    private async Task RunCoderLoop(Workspace workspace, Model model, AgentTeam team, CompileResult compileResult)
    {
        while (workspace.NeedsCoder(compileResult))
        {
            await AgentFlow(workspace, model, team.Coder);
            compileResult = await workspace.Compile();
        }
    }
    private async Task RunCodeReviewLoop(Workspace workspace, Model model, AgentTeam team)
    {
        while (workspace.NeedsCodeReview())
        {
            await AgentFlow(workspace, model, team.CodeReviewer);
        }
    }

    private async Task AgentFlow(Workspace workspace, Model model, IAgent agent)
    {
        var hasToolCalls = false;
        while (!hasToolCalls)
        {
            var historyItem = new WorkspaceHistory()
            {
                AgentName = agent.AgentName,
                DateTime = DateTime.Now
            };
            workspace.History.Add(historyItem);
            hasToolCalls = await Run(workspace, model, agent, historyItem);
        }
    }

    private async Task<bool> Run(Workspace workspace, Model model, IAgent agent, WorkspaceHistory historyItem)
    {
        var hasToolCalls = false;
        Console.Clear();
        Console.WriteLine("\x1b[3J");

        if (historyItem.ApiCall == null)
        {
            historyItem.ApiCall = await agent.GenerateApiCall();
            await workspace.Save();
        }

        foreach (var message in historyItem.ApiCall.Messages)
            ShowMessage(message);
        Console.WriteLine();

        if (historyItem.Response == null)
        {
            historyItem.Response = await Client.ChatAsync(model, historyItem.ApiCall);
        }

        ShowMessage(historyItem.Response.message);
        Console.WriteLine();

        if (historyItem.ResponseResults == null)
        {
            historyItem.ResponseResults = await agent.ProcessResponse(historyItem.ApiCall, historyItem.Response);
            hasToolCalls = historyItem.ResponseResults.ToolCallResults.Any(a => a.result.error == false);
            await workspace.Save();

            if (await PerformFlagActions(workspace))
            {
                await workspace.Save();
            }
        }
        
        return hasToolCalls;
    }
    private async Task<bool> PerformFlagActions(Workspace workspace)
    {
        var response = false;
        if (workspace.Flags.NeedClearCodingHistoryFlag)
        {
            workspace.CodingHistory.Clear();
            workspace.Flags.NeedClearCodingHistoryFlag = false;
            response = true;
        }
        if (workspace.Flags.NeedClearDebugHistoryFlag)
        {
            workspace.DebugHistory.Clear();
            workspace.Flags.NeedClearDebugHistoryFlag = false;
            response = true;
        }
        return response;
    }

    private static async Task<Workspace> CreateWorkspace(string workspaceDirectory)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Please supply a apiCall, what do you want to create (use CTRL + enter to submit):");
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
            if (int.TryParse(numberString, out var number))
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

                if (!string.IsNullOrWhiteSpace(call.Function.Arguments.Action))
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