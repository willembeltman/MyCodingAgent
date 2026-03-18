using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;

namespace MyCodingAgent.ToolCalls;

public class Ask_HumanDeveloper_Tool(Workspace workspace) : IToolCall
{
    public string Name
        => "ask_human_developer";
    public string Description
        => "Asks the human developer using this coding agent for additional information when the development cannot continue";
    public ToolParameter[] Parameters { get; } =
    [
        new ("content", "string", "question or information request for the human developer")
    ];
    public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    {
        var toolArguments = toolCall.function.arguments;
        if (toolArguments.content == null)
            return new ToolResult(
                "parameter content is not supplied",
                "parameter content is not supplied",
                true);

        if (toolCall.id == null)
            throw new Exception("eeeuhm..");

        workspace.CodingAgent_To_ProjectManagerAgent_Question =
            new(toolCall.id, toolArguments.content);

        var answer = "Waiting for answer..";
        return new ToolResult(
            answer,
            answer,
            false);
    }
    //public async Task<ToolResult> Invoke(OllamaToolCall toolCall)
    //{
    //    var toolArguments = toolCall.function.arguments;
    //    if (toolArguments.content == null)
    //        return new ToolResult(
    //            "parameter content is not supplied",
    //            "parameter content is not supplied",
    //            true);

    //    var previousColor = Console.ForegroundColor;

    //    Console.ForegroundColor = ConsoleColor.White;
    //    Console.WriteLine();
    //    Console.WriteLine("The llm model want more information, can you answer his question?");
    //    Console.WriteLine("The question:");
    //    Console.WriteLine(toolArguments.content);
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
    //}
}