namespace MyCodingAgent.Models;

public record ApiCall(
    Message[] Messages,
    Tool[] Tools);
