using MyCodingAgent.Models;

namespace MyCodingAgent.Interfaces;

public interface IFileRepository
{
    //List <WorkspaceOriginalFile> OriginalFiles { get; }
    string GetRootDirectoryName(Workspace workspace);
    IEnumerable<WorkspaceEvent> GetEvents(Workspace workspace);
}