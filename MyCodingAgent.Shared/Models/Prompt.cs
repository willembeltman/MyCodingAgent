namespace MyCodingAgent.Shared.Models;

public record Prompt(
    Message[] messages,
    Tool[] tools);
