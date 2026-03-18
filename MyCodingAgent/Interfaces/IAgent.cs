using MyCodingAgent.Models;
using MyCodingAgent.Shared.Models;

namespace MyCodingAgent.Interfaces;

public interface IAgent
{
    Task<Prompt> GeneratePrompt();
    Task<bool> ProcessResponse(Prompt prompt, Response agentResponse);
}
