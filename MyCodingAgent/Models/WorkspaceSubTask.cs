namespace MyCodingAgent.Models;

public class WorkspaceSubTask(
    int id, 
    string content)
{
    public int Id { get; set; } = id;
    public string Content { get; set; } = content;
    public bool Finished { get; set; }
}