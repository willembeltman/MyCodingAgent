namespace MyCodingAgent.OllamaClient;

public record OllamaModelRaw(
    string? name,
    long? size,
    string? digest,
    DateTime? modified_at);