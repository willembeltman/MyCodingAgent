namespace MyCodingAgent.OllamaClient;

public record OllamaTool(
    string Name,
    string Desciption,
    OllamaToolParameter[] Parameters);
