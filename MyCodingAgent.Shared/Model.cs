namespace MyCodingAgent.Shared;

public record Model(
    string Name, 
    long? MemorySize, 
    int? MaxTokenSize,
    DateTime? LastModified);