using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace vs.Architect.Client.SolutionMinder.ConsoleApp.Helpers;

public static class SolutionAnalyzer
{
    public static async Task<Solution> LoadSolutionAsync(FileInfo solution)
    {
        var workspace = MSBuildWorkspace.Create();
        return await workspace.OpenSolutionAsync(solution.FullName);
    }
}