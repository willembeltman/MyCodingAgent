namespace MyCodingAgent.OllamaClient.Models;

internal record OllamaResponse(
    string model,
    DateTime created_at,
    OllamaMessage message);
