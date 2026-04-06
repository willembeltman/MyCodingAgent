namespace MyCodingAgent.Models;

public record LlmResponse(
    string model,
    DateTime created_at,
    Message message);
