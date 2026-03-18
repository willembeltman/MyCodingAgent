namespace MyCodingAgent.Models;

public class WorkspaceFile(
    string relativePath,
    string fullPath)
{
    public string RelativePath { get; set; } = relativePath;
    public string FullPath { get; set; } = fullPath;

    public Task<string> GetFileContent()
    {
        return File.ReadAllTextAsync(FullPath);
    }
    public async Task UpdateContent(string content)
    {
        var fileInfo = new FileInfo(FullPath);
        if (fileInfo.Directory == null)
            throw new Exception($"Weird stuff, directory is empty? {fileInfo}");
        if (fileInfo.Directory.Exists == false)
            fileInfo.Directory.Create();

        await File.WriteAllTextAsync(FullPath, content);
    }
    public async Task UpdateContent(int startLine, int endLine, string newContent)
    {
        var fileContent = await GetFileContent();
        var lines = fileContent.Split('\n').ToList();
        var newLines = newContent.Split('\n');

        if (endLine >= 0)
            lines.RemoveRange(startLine - 1, endLine - startLine + 1);
        lines.InsertRange(startLine - 1, newLines);

        var content = string.Join("\n", lines);
        await File.WriteAllTextAsync(FullPath, content);
    }
    public bool Exists()
    {
        return File.Exists(FullPath);
    }
    public void Delete()
    {
        File.Delete(FullPath);
    }
    public void Move(string newPath, string newFullPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(newFullPath)!);
        File.Move(FullPath, newFullPath, true);
        RelativePath = newPath;
        FullPath = newFullPath;
    }
}
