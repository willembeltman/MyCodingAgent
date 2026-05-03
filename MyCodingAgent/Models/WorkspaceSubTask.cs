using System.ComponentModel.DataAnnotations;

namespace MyCodingAgent.Models;

public record WorkspaceSubTask(
    long Id, 
    string Content)
{
    public bool Finished { get; set; }
}