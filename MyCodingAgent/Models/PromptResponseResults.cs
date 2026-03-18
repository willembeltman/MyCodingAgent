using MyCodingAgent.Shared;

namespace MyCodingAgent.Models;

public record PromptResponseResults(
    Prompt Prompt,
    Response Response,
    List<ToolCallResult> ToolCallResults);