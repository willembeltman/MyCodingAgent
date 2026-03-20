using MyCodingAgent.Enums;
using MyCodingAgent.Models;

namespace MyCodingAgent.Interfaces;

public interface IAgent
{
    AgentType AgentName { get; }

    Task<ApiCall> GenerateApiCall();
    Task<ResponseResults> ProcessResponse(ApiCall apiCall, Response agentResponse);
}
