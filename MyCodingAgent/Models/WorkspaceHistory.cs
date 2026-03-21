using MyCodingAgent.Enums;

namespace MyCodingAgent.Models;

public class WorkspaceHistory
{
    public DateTime DateTime {get; init; } = default!;
    public AgentType AgentName { get; init; } = default!;
    public ApiCall? ApiCall { get; set; }
    public Response? Response { get; set; }
    public ResponseResults? ResponseResults { get; set; }
}