namespace MyCodingAgent.Models;

public record WorkspaceOriginalFile(
    string RelativePath,
    string FullPath,
    string Content);