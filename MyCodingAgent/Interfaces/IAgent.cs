using MyCodingAgent.Enums;
using MyCodingAgent.Models;

namespace MyCodingAgent.Interfaces;

public interface IAgent
{
    Actor AgentName { get; }
    long StartPoint { get; }

    Task<LlmRequest> GenerateRequest(CompileResult compileResult);
    Task<SystemResult> ProcessResponse(LlmRequest request, LlmResponse response);
}
