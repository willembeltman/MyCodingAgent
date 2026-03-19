using MyCodingAgent.Models;

namespace MyCodingAgent.Interfaces;

public interface IAgent
{
    Task<Prompt> GeneratePrompt();
    Task<bool> ProcessResponse(Prompt prompt, Response agentResponse);
}
