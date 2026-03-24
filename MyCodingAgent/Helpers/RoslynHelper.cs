using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace MyCodingAgent.Helpers;

public class RoslynHelper
{
    public async Task<Compilation?> GetCompilation(FileInfo projectFile)
    {
        // Maak een workspace die MSBuild begrijpt
        var workspace = MSBuildWorkspace.Create();

        // Open het project. Roslyn regelt alle references en imports op basis van de .csproj
        var project = await workspace.OpenProjectAsync(projectFile.FullName);

        // Nu heb je een volledige compilatie inclusief alle Razor-gerelateerde metadata
        var compilation = await project.GetCompilationAsync();

        return compilation;
    }
}
