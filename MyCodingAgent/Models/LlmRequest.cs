namespace MyCodingAgent.Models;

public record LlmRequest(
    Message[] Messages,
    Tool[] Tools);
