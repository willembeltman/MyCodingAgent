using MyCodingAgent.Models;
using MyCodingAgent.Shared;

namespace MyCodingAgent.Interfaces;

public interface IToolCall
{
    string Name {  get; }
    string Description { get; }
    ToolParameter[] Parameters { get; }
    Task<ToolResult> Invoke(ToolCall toolArguments);
    public Tool ToDto()
    {
        return new Tool(Name, Description, Parameters);
    }
}