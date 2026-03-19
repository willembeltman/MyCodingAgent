using MyCodingAgent.Shared.Models;

namespace MyCodingAgent.Shared.Interfaces;

public interface IAgent
{
    Task<Prompt> GeneratePrompt();
    Task<bool> ProcessResponse(Prompt prompt, Response agentResponse);
}
