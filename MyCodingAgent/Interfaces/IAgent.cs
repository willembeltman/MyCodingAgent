using MyCodingAgent.Models;
using MyCodingAgent.Shared;

namespace MyCodingAgent.Interfaces;

public interface IAgent
{
    Task<Prompt> GeneratePrompt();
    Task<bool> ProcessResponse(Prompt prompt, Response agentResponse);
}
