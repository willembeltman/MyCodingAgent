using Microsoft.Extensions.Configuration;
using MyCodingAgent;
using MyCodingAgent.Agents;
using MyCodingAgent.Enums;
using MyCodingAgent.Extentions;
using MyCodingAgent.Factories;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.OllamaClient;
using System.Reflection;

#pragma warning disable CA1859 // Use concrete types when possible for improved performance

internal class Program : IDisposable
{
    readonly CancellationTokenSource Cts;
    //readonly IClient Client;
    readonly Dictionary<(Actor from, Actor to), Func<AgentTeam, IEmailableAgent>> EmailableAgents = new()
    {
        { (Actor.Coder, Actor.ProjectManager), team => team.ProjectManagerForCoder },
        { (Actor.Debugger, Actor.ProjectManager), team => team.ProjectManagerForDebugger },
        { (Actor.Debugger, Actor.Coder), team => team.CoderForDebugger },
    };

    private Program()
    {
        Console.Clear();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("MyCodingAgent v0.002, created by Willem-Jan Beltman");
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
        var workspace = await WorkspaceFactory.TryLoad(workspaceDirectory);
        if (workspace == null)
            workspace = await WorkspaceFactory.Create(workspaceDirectory);

        var workspaceTask = workspace.GetCurrentTask();
        if (workspaceTask == null || workspaceTask.Flags.TaskIsDoneFlag == true)
            workspaceTask = await CreateWorkspaceTask(workspace);

        Console.WriteLine("Workspace loaded, getting model list, please wait...");
        var modelList = await Client.GetModels();
        var model = ChooseModel(modelList);

        Console.WriteLine($"Initialising model '{model.Name}', please wait...");
        await Client.InitializeModelAsync(model);

        var current = new Current(Client, model, workspace, workspaceTask);

        await RunMainLoop(current);
    }
    private async Task RunMainLoop(Current current)
    {
        Console.Clear();

        Console.WriteLine($"Model '{current.Model.Name}' initialized, initialising agents, please wait while we initialize the agent team...");
        
        var team = new AgentTeam(current);

        Console.WriteLine("Agents initialized, starting lllm-development-cycle, please wait...");

        while (!current.IsDone)
        {
            // Elke pass doen we een compile
            var compileResult = await current.Compile();

            if (current.NeedsPlanner())
            {
                // PLANNING MODE
                await RunPlanningLoop(current, team, compileResult);
                continue;
            }
            if (current.HasInboxMessages())
            {
                // MESSAGE BETWEEN AGENTS
                await RunInboxLoop(current, team, compileResult);
                continue;
            }
            if (current.NeedsDebugging(compileResult))
            {
                // DEBUGGER MODE
                await RunDebuggerLoop(current, team, compileResult);
                continue;
            }
            if (current.NeedsCoder(compileResult))
            {
                // CODER MODE
                await RunCoderLoop(current, team, compileResult);
                continue;
            }
            if (current.NeedsCodeReview())
            {
                // CODE REVIEW MODE
                await RunCodeReviewLoop(current, team, compileResult);
            }
        }

        await current.Workspace.Save();
    }

    private async Task RunPlanningLoop(Current current, AgentTeam team, CompileResult compileResult)
    {
        while (current.NeedsPlanner())
        {
            await AgentFlow(current, team.Planner, compileResult);
        }
        await current.Workspace.Save();
    }
    private async Task RunInboxLoop(Current current, AgentTeam team, CompileResult compileResult)
    {
        var message = current.Task.InboxMessages.LastOrDefault() ??
            throw new Exception("Er gaat iets mis in de flow, waarom wordt deze functie aangeroepen als er geen messages in de inbox staan.");
        if (!EmailableAgents.TryGetValue((message.From, message.To), out var emailableAgentGetter))
            throw new Exception("Er gaat iets mis in de flow, waarom wordt deze functie aangeroepen met een niet bekende from/to.");
        var emailableAgent = emailableAgentGetter(team);
        emailableAgent.SetCurrentMessage(message);
        while (current.Task.InboxMessages.LastOrDefault() == message)
        {
            await AgentFlow(current, emailableAgent, compileResult);
        }
    }
    private async Task RunDebuggerLoop(Current current, AgentTeam team, CompileResult compileResult)
    {
        while (current.NeedsDebugging(compileResult))
        {
            await AgentFlow(current, team.Debugger, compileResult);
            compileResult = await current.Compile();
        }
    }
    private async Task RunCoderLoop(Current current, AgentTeam team, CompileResult compileResult)
    {
        while (current.NeedsCoder(compileResult))
        {
            await AgentFlow(current, team.Coder, compileResult);
            compileResult = await current.Compile();
        }
    }
    private async Task RunCodeReviewLoop(Current current, AgentTeam team, CompileResult compileResult)
    {
        while (current.NeedsCodeReview())
        {
            await AgentFlow(current, team.CodeReviewer, compileResult);
        }
    }

    private async Task AgentFlow(Current current, IAgent agent, CompileResult compileResult)
    {
        var hasToolCalls = false;
        while (!hasToolCalls)
        {
            var historyItem = new WorkspaceEvent()
            {
                Actor = agent.AgentName,
                TimeStamp = DateTime.Now,
                CompileResult = compileResult
            };
            current.Workspace.Events.Add(historyItem);
            hasToolCalls = await Run(current, agent, historyItem);
        }
    }

    private async Task<bool> Run(Current current, IAgent agent, WorkspaceEvent historyItem)
    {
        var hasToolCalls = false;
        Console.Clear();
        Console.WriteLine("\x1b[3J");

        if (historyItem.Request == null)
        {
            historyItem.Request = await agent.GenerateRequest(historyItem.CompileResult);
            await current.Workspace.Save();
        }

        foreach (var message in historyItem.Request.Messages)
            ShowMessage(message);
        Console.WriteLine();

        if (historyItem.Response == null)
        {
            historyItem.Response = await Client.ChatAsync(current.Model, historyItem.Request);
        }

        ShowMessage(historyItem.Response.message);
        Console.WriteLine();

        if (historyItem.ToolCallResults == null)
        {
            historyItem.ToolCallResults = await agent.ProcessResponse(historyItem.Request, historyItem.Response);
            hasToolCalls = historyItem.ToolCallResults.Any(a => a.Result.Error == false);
            await current.Workspace.Save();
        }
        
        return hasToolCalls;
    }

    private static async Task<WorkspaceTask> CreateWorkspaceTask(Workspace workspace)
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
        Console.ForegroundColor = previousColor;
        var workspaceTask = await workspace.CreateTask(userPromptText);
        return workspaceTask;
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

                Console.ForegroundColor = ConsoleColor.Red;
                if (!string.IsNullOrWhiteSpace(call.Function.Arguments.Action))
                    Console.WriteLine($"action: {call.Function.Arguments.Action.ToUpper()}");

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