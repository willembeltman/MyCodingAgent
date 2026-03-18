using MyCodingAgent.Shared.Models;

namespace MyCodingAgent.Models;

public record PromptResponseResults(
    Prompt Prompt,
    Response Response,
    List<ToolCallResult> ToolCallResults);