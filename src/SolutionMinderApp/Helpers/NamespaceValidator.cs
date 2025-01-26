using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace vs.Architect.Client.SolutionMinder.ConsoleApp.Helpers;

public static class NamespaceValidator
{
    public static CompilationUnitSyntax EnsureNamespace(CompilationUnitSyntax root, string inheritorType, params string[] namespaces)
    {
        var hasInheritorType = root
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Any(classDeclaration => classDeclaration.BaseList?.Types.Any(t => t.Type.ToString() == inheritorType) == true);

        if(! hasInheritorType)
            return root;

        foreach(var ns in namespaces)
        {
            if(root.Usings.All(u => u.Name?.ToString() != ns))
            {
                root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)));
            }
        }
        return root;

    }
}
