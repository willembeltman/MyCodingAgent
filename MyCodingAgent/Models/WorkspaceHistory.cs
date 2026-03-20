namespace MyCodingAgent.Models;

public class WorkspaceHistory
{
    public string AgentName { get; init; } = default!;
    public ApiCall? ApiCall { get; set; }
    public Response? Response { get; set; }
    public ResponseResults? ResponseResults { get; set; }
}