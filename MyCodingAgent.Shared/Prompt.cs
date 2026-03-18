namespace MyCodingAgent.Shared;

public record Prompt(
    Message[] messages,
    Tool[] tools);
