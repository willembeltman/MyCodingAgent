using MyCodingAgent.Interfaces;
using MyCodingAgent.Models;
using MyCodingAgent.Enums;

namespace MyCodingAgent.ToolCalls;

public class AgentToAgent_Question_Tool(
    Workspace workspace,
    AgentType from,
    AgentType to,
    string name,
    string description,
    string contentParameterDescription) 
    : IToolCall
{
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
    public ToolParameter[] Parameters { get; } =
    [
        new ("content", "string", contentParameterDescription)
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
            new(toolCall.Id, from, to, toolArguments.Content));

        var answer = "Waiting for answer..";
        return new ToolResult(
            answer,
            answer,
            false);
    }
}