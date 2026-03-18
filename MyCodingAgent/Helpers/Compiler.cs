using MyCodingAgent.Models;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace MyCodingAgent.Helpers;

public static class Compiler
{
    public static async Task<CompileResult> Compile(DirectoryInfo currentDirectory)
    {
        var slnFile = currentDirectory.GetFiles("*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
        var slnxFile = currentDirectory.GetFiles("*.slnx", SearchOption.TopDirectoryOnly).FirstOrDefault();
        var csprojFile = currentDirectory.GetFiles("*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        var fileToBuild = slnFile ?? slnxFile ?? csprojFile;
        if (fileToBuild == null)
            return new CompileResult()
            {
                Content = "No .sln, .slnx or .csproj file was found in ROOT of workspace.",
                Errors = [
                    new CompileError("No .sln or .csproj file was found in ROOT of workspace.")
                ]
            };
        return await Compile(fileToBuild);
    }

    public static async Task<CompileResult> Compile(FileInfo solutionOrProjectFile)
    {
        DirectoryInfo currentDirectory = solutionOrProjectFile.Directory!;
        string fileToBuild = solutionOrProjectFile.FullName;
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{fileToBuild}\" -nologo -v:m",
            WorkingDirectory = currentDirectory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };


        var result = new CompileResult();

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        string output = outputBuilder.ToString();

        // Try to extract compiler errors using regex
        // Example line: "Program.cs(10,5): error CS1002: ; expected [MyApp -> MyApp.csproj]"
        var matches = Regex.Matches(output, @"^(?<file>.+?)\((?<line>\d+),(?<col>\d+)\):\s*(?<type>error|warning)\s*(?<code>CS\d+):\s*(?<msg>.+)$",
            RegexOptions.Multiline);

        if (matches.Count > 0)
        {
            // Errors matched
            var formatted = new StringBuilder();
            formatted.AppendLine($"Build failed ({matches.Count} issue{(matches.Count == 1 ? "" : "s")}):");

            foreach (Match m in matches)
            {
                var fullError = $"[{m.Groups["type"].Value.ToUpper()} {m.Groups["code"].Value}] {m.Groups["file"].Value}({m.Groups["line"].Value},{m.Groups["col"].Value}): {m.Groups["msg"].Value}"
                    .Replace(currentDirectory.FullName + "\\", "");
                var error = new CompileError(
                    fullError,
                    m.Groups["type"].Value,
                    m.Groups["code"].Value
                        .Replace(currentDirectory.FullName + "\\", ""),
                    m.Groups["file"].Value
                        .Replace(currentDirectory.FullName + "\\", ""),
                    m.Groups["line"].Value,
                    m.Groups["col"].Value,
                    m.Groups["msg"].Value
                        .Replace(currentDirectory.FullName + "\\", ""));

                formatted.AppendLine(fullError);
                if (error.Type?.ToLower() == "warning")
                    result.Warnings.Add(error);
                else
                    result.Errors.Add(error);
            }

            result.Content = formatted
                .ToString();
        }
        else if (process.ExitCode == 0)
        {
            // Build succesful
            var formatted = output
                .Trim()
                .Replace(currentDirectory.FullName + "\\", "");
            result.Content = $"Build successful.\n{formatted}";
        }
        else
        {
            // No regex matches but nonzero exit: return raw logs
            var formatted = $"Build failed (exit code {process.ExitCode}).\n{output}"
                .Replace(currentDirectory.FullName + "\\", "");

            result.Content = formatted;
            result.Errors.Add(new CompileError(formatted));
        }

        return result;
    }
}
