namespace MyCodingAgent.OllamaClient.Models;

internal record OllamaTool(
    string Name,
    string Desciption,
    OllamaToolParameter[] Parameters);
