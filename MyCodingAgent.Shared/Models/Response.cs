namespace MyCodingAgent.Shared.Models;

public record Response(
    string model,
    DateTime created_at,
    Message message);
