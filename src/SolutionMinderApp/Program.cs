using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using vs.Architect.Client.SolutionMinder.ConsoleApp.Helpers;
using vs.Architect.Client.SolutionMinder.ConsoleApp;

var selector = new SolutionSelector();
var (selection, selectedOption) = selector.SelectSolution();

if(selectedOption == SolutionSelector.SelectionOption.Exit)
{
    Console.WriteLine("No solution selected. Exiting.");
    Console.ReadLine();
    Environment.Exit(0);
}

if(selection is null)
{
    Console.WriteLine("Invalid solution selected. Exiting.");
    Console.ReadLine();
    Environment.Exit(0);
}

Console.WriteLine($"Selected solution: {selection.Name}");

Console.WriteLine("Solution loading...");
var solution = await SolutionAnalyzer.LoadSolutionAsync(selection);
Console.WriteLine("Loaded.");
Console.WriteLine();

var projects = solution.Projects.ToList();
Console.WriteLine($"{projects.Count} projects loaded.");

var includeProjects = Settings.Default.IncludeProjects.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
projects = projects.Where(p => includeProjects.Any(t => p.Name.Contains(t))).ToList();

var excludeProjects = Settings.Default.ExcludeProjects.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
projects = projects.Where(p => !excludeProjects.Any(t => p.Name.Contains(t))).ToList();

Console.WriteLine($"{projects.Count} projects to be reviewed.");
foreach(var project in projects)
{

    Console.WriteLine($"Project: {project.Name}");
    foreach(var document in project.Documents)
    {

        var fi = new FileInfo(document.FilePath ?? string.Empty);

        // Basic check
        if(!fi.Exists)
            continue;

        // Ignore files that do not have a .cs extension
        if(! fi.Extension.Equals(".cs", StringComparison.CurrentCulture))
            continue;

        // Ignore generated files
        if(fi.FullName.EndsWith(".g.cs"))
            continue;

        // Ignore files that start with .NETCoreApp
        if(fi.Name.StartsWith(".NETCoreApp", StringComparison.CurrentCulture))
            continue;

        // Ignore AssemblyInfo files
        if(fi.Name.Contains("AssemblyInfo.cs", StringComparison.CurrentCulture))
            continue;

        var syntaxRoot = await document.GetSyntaxRootAsync();
        if(syntaxRoot is not CompilationUnitSyntax root)
            continue;

        var semanticModel = await document.GetSemanticModelAsync();
        if(semanticModel == null)
            continue;

        Console.WriteLine($"\tFile: {document.Name}");

        var originalContent = root.NormalizeWhitespace().ToFullString();
        var cachedRootContent = root.ToFullString();
        root = NamespaceValidator.EnsureNamespace(root, "DataContractBase", "System.Runtime.Serialization");
        if(cachedRootContent != root.ToFullString())
            Console.WriteLine("\tUsings change.");

        cachedRootContent = root.ToFullString();
        root = AttributeEnforcer.EnsureClassAttributes(root, semanticModel, "DataContractBase", nameof(DataContractAttribute), nameof(DataMemberAttribute));
        if(cachedRootContent != root.ToFullString())
            Console.WriteLine("\tClass attributes modified.");

        cachedRootContent = root.ToFullString();
        root = NamespaceValidator.EnsureNamespace(root, "IServiceContract", "System.ServiceModel");
        if(cachedRootContent != root.ToFullString())
            Console.WriteLine("\tUsings change.");

        cachedRootContent = root.ToFullString();
        root = AttributeEnforcer.EnsureInterfaceAttributes(root, semanticModel, "IServiceContract", nameof(ServiceContractAttribute), nameof(OperationContractAttribute));
        if(cachedRootContent != root.ToFullString())
            Console.WriteLine("\tInterface change.");

        var updatedContent = root.NormalizeWhitespace().ToFullString();
        if(originalContent != updatedContent)
        {
            File.WriteAllText(fi.FullName, updatedContent, Encoding.Default);
            Console.WriteLine("\tFile updated.");
        }

    }

}

Console.WriteLine("Finished.");
Console.ReadLine();
return 0;
