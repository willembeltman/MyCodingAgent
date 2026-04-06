using MyCodingAgent.Enums;

namespace MyCodingAgent.Models;

public class WorkspaceFile
{
    public WorkspaceFile(string relativePath)
    {
        _OriginalRelativePath = relativePath;
    }
    //public WorkspaceFile(WorkspaceOriginalFile originalFile)
    //{
    //    _OriginalRelativePath = originalFile.RelativePath;
    //    _OriginalFile = originalFile;
    //}

    private string _OriginalRelativePath { get; }
    //private WorkspaceOriginalFile? _OriginalFile { get; }
    private List<IoOperation> _IoOperations { get; } = [];

    private string? _RelativePath = null;
    private string? _Content = null;
    public string RelativePath
    {
        get
        {
            if (_RelativePath != null)
                return _RelativePath;

            _RelativePath = _OriginalRelativePath;
            foreach (var ioOperation in _IoOperations)
            {
                if (ioOperation.Type == IoOperationType.Move &&
                    ioOperation.NewPath != null)
                {
                    _RelativePath = ioOperation.NewPath;
                }
            }
            return _RelativePath;
        }
    }

    public Task<string> GetContent()
    {
        if (_Content != null)
            return Task.FromResult(_Content);

        //_Content = _OriginalFile?.Content ?? string.Empty;
        _Content = string.Empty;
        foreach (var ioOperation in _IoOperations)
        {
            if (ioOperation.Type == IoOperationType.Append &&
                ioOperation.Content != null)
            {
                _Content = ioOperation.Content;
            }
        }
        return Task.FromResult(_Content);
    }

    public void AddIoOperation(IoOperation ioOperation)
    {
        _IoOperations.Add(ioOperation);
        _RelativePath = null;
        _Content = null;
    }
}

//public class WorkspaceFile(
//    string relativePath,
//    string fullPath)
//{
//    public string RelativePath { get; set; } = relativePath;
//    public string FullPath { get; set; } = fullPath;

//    public Task<string> GetFileContent()
//    {
//        return File.ReadAllTextAsync(FullPath);
//    }
//    public async Task UpdateContent(string content)
//    {
//        var fileInfo = new FileInfo(FullPath);
//        if (fileInfo.Directory == null)
//            throw new Exception($"Weird stuff, directory is empty? {fileInfo}");
//        if (fileInfo.Directory.Exists == false)
//            fileInfo.Directory.Create();

//        await File.WriteAllTextAsync(FullPath, content);
//    }
//    public async Task UpdateContent(int startLine, int endLine, string newContent)
//    {
//        var fileContent = await GetFileContent();
//        var lines = fileContent.Split('\n').ToList();
//        var newLines = newContent.Split('\n');

//        if (endLine >= 0)
//            lines.RemoveRange(startLine - 1, endLine - startLine + 1);
//        lines.InsertRange(startLine - 1, newLines);

//        var content = string.Join("\n", lines);
//        await File.WriteAllTextAsync(FullPath, content);
//    }
//    public bool Exists()
//    {
//        return File.Exists(FullPath);
//    }
//    public void Delete()
//    {
//        File.Delete(FullPath);
//    }
//    public void Move(string newPath, string newFullPath)
//    {
//        Directory.CreateDirectory(Path.GetDirectoryName(newFullPath)!);
//        File.Move(FullPath, newFullPath, true);
//        RelativePath = newPath;
//        FullPath = newFullPath;
//    }
//}
