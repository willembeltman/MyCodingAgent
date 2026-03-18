namespace MyCodingAgent.Models;

public record OllamaModelRaw(
    string? name,
    long? size,
    string? digest,
    DateTime? modified_at);