namespace vs.Architect.Client.SolutionMinder.ConsoleApp.Helpers;

public static class SolutionExtension
{
    public static bool HasMatchingExtension(this FileInfo fileInfo, string extension)
    {
        return fileInfo.Exists && fileInfo.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase);
    }
}