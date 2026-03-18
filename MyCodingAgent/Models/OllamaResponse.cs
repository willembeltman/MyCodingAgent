namespace MyCodingAgent.Models;

public record OllamaResponse(
    string model,
    DateTime created_at,
    OllamaMessage message);
