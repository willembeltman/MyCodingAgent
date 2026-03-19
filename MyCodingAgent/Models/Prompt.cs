namespace MyCodingAgent.Models;

public record Prompt(
    Message[] messages,
    Tool[] tools);
