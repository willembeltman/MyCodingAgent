namespace MyCodingAgent.Models;

public record Response(
    string model,
    DateTime created_at,
    Message message);
