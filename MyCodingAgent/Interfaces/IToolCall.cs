using MyCodingAgent.Models;

namespace MyCodingAgent.Interfaces;

public interface IToolCall
{
    string Name {  get; }
    string Description { get; }
    ToolParameter[] Parameters { get; }
    Task<ToolResult> Invoke(OllamaToolCall toolArguments);
    public Tool ToDto()
    {
        return new Tool(Name, Description, Parameters);
    }
}