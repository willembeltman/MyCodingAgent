using MyCodingAgent.Enums;
using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls;

public class AskHumanDeveloper_Question_Tool(
    Workspace workspace,
    AgentType agent)
     : IToolCall
{
    public string Name
        => "ask_human_developer";
    public string Description
        => "Asks the human developer using this coding agent for additional information when the development cannot continue";
    public ToolParameter[] Parameters { get; } =
    [
        new ("content", "string", "question or information request for the human developer")
    ];


    public async Task<ToolResult> Invoke(ToolCall toolCall)
    {
        var toolArguments = toolCall.Function.Arguments;
        if (toolArguments.Content == null)
            return new ToolResult(
                "parameter content is not supplied",
                "parameter content is not supplied",
                true);

        if (toolCall.Id == null)
            throw new Exception("eeeuhm..");

        workspace.InboxMessages.Add(
            new(toolCall.Id, agent, AgentType.Human, toolArguments.Content));

        var answer = "Waiting for answer..";
        return new ToolResult(
            answer,
            answer,
            false);
    }

    // public async Task<ToolResult> Invoke(ToolCall toolCall)
    // {
    //    var toolArguments = toolCall.Function.Arguments;
    //    if (toolArguments.Content == null)
    //        return new ToolResult(
    //            "parameter content is not supplied",
    //            "parameter content is not supplied",
    //            true);

    //    var previousColor = Console.ForegroundColor;

    //    Console.ForegroundColor = ConsoleColor.White;
    //    Console.WriteLine();
    //    Console.WriteLine("The llm model want more information, can you answer his question?");
    //    Console.WriteLine("The question:");
    //    Console.WriteLine(toolArguments.Content);
    //    Console.WriteLine();
    //    Console.WriteLine("Your answer:");
    //    var answer = ConsoleEditor.ReadMultilineInput();
    //    Console.WriteLine();
    //    Console.WriteLine();

    //    Console.ForegroundColor = previousColor;

    //    return new ToolResult(
    //        answer,
    //        answer,
    //        false);
    // }
}