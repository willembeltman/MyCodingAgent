namespace MyCodingAgent.Models;

public class CompileError
{
    public CompileError(string fullError)
    {
        FullError = fullError;
    }

    public CompileError(
        string fullError,
        string type, 
        string code, 
        string file, 
        string line, 
        string col,
        string msg) : this(fullError)
    {
        Type = type;
        Code = code;
        File = file;
        Line = line;
        Col = col;
        Msg = msg;
    }

    public string FullError { get; }
    public string? Type { get; }
    public string? Code { get; }
    public string? File { get; }
    public string? Line { get; }
    public string? Col { get; }
    public string? Msg { get; }
}