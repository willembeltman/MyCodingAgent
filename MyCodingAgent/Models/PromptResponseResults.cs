namespace MyCodingAgent.Models;

public record PromptResponseResults(
    OllamaPrompt Prompt,
    OllamaResponse Response,
    List<ToolCallResult> ToolCallResults);