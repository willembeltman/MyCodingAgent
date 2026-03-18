namespace MyCodingAgent.OllamaClient.Models;

internal record OllamaModelRaw(
    string? name,
    long? size,
    string? digest,
    DateTime? modified_at);