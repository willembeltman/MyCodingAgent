using MyCodingAgent.Enums;
using MyCodingAgent.Models;

namespace MyCodingAgent.Interfaces;

public interface IAgent
{
    Actor AgentName { get; }

    Task<LlmRequest> GenerateRequest(CompileResult compileResult);
    Task<ToolCallResult[]> ProcessResponse(LlmRequest request, LlmResponse response);
}
