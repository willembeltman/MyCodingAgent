using MyCodingAgent.Models;
using MyCodingAgent.Shared.Models;

namespace MyCodingAgent.Shared.Interfaces;

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