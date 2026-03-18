using MyCodingAgent.Models;

namespace MyCodingAgent.Interfaces;

public interface IAgent
{
    Task<OllamaPrompt> GeneratePrompt();
    Task<bool> ProcessResponse(OllamaPrompt prompt, OllamaResponse agentResponse);
}
