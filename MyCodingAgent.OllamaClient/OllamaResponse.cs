namespace MyCodingAgent.OllamaClient;

public record OllamaResponse(
    string model,
    DateTime created_at,
    OllamaMessage message);
