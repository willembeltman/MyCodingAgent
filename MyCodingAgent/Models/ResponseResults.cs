using MyCodingAgent.Models;

namespace MyCodingAgent.Models;

public record ResponseResults(
    ApiCall Prompt,
    Response Response,
    List<ToolCallResult> ToolCallResults);