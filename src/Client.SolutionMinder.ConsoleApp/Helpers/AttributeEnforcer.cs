using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace vs.Architect.Client.SolutionMinder.ConsoleApp.Helpers;

public static class AttributeEnforcer
{
    public static CompilationUnitSyntax EnsureClassAttributes(CompilationUnitSyntax root, SemanticModel semanticModel, string baseClassName, string classAttributeName, string propertyAttributeName)
    {
        foreach(var item in ClassesWithMatchingBase(root, semanticModel, baseClassName))
        {
            root = AddClassAttributes(root, item, classAttributeName);
            root = AddPropertyAttributes(root, item, propertyAttributeName);
        }
        return root;
    }

    public static CompilationUnitSyntax EnsureInterfaceAttributes(CompilationUnitSyntax root, SemanticModel semanticModel, string baseInterfaceName, string interfaceAttributeName, string methodAttributeName)
    {
        foreach(var item in InterfacesWithMatchingBase(root, semanticModel, baseInterfaceName))
        {
            root = AddInterfaceAttribute(root, item, interfaceAttributeName);
            root = AddMethodAttributes(root, item, methodAttributeName);
        }
        return root;
    }

    private static IEnumerable<ClassDeclarationSyntax> ClassesWithMatchingBase(CompilationUnitSyntax root, SemanticModel semanticModel, string baseClassName)
    {
        return root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c =>
            {
                var symbol = semanticModel.GetDeclaredSymbol(c);
                return symbol?.BaseType?.ToString() == baseClassName;
            });
    }
    
    private static IEnumerable<InterfaceDeclarationSyntax> InterfacesWithMatchingBase(CompilationUnitSyntax root, SemanticModel semanticModel, string baseInterfaceName)
    {
        return root.DescendantNodes()
            .OfType<InterfaceDeclarationSyntax>()
            .Where(i =>
            {
                var symbol = semanticModel.GetDeclaredSymbol(i);
                return symbol?.Interfaces.Any(j => j.ToString() == baseInterfaceName) == true;
            });
    }

    private static CompilationUnitSyntax AddClassAttributes(CompilationUnitSyntax root, ClassDeclarationSyntax node, string attributeName)
    {
        if(HasAttribute(node, attributeName))
            return root;

        var updatedClass = node.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName(attributeName)))));
        return root.ReplaceNode(node, updatedClass);
    }

    private static CompilationUnitSyntax AddMethodAttributes(CompilationUnitSyntax root, InterfaceDeclarationSyntax interfaceDecl, string attributeName)
    {
        foreach(var method in MethodsWithoutAttributes(interfaceDecl, attributeName))
        {
            var newMethod = method.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName(attributeName)))));
            root = root.ReplaceNode(method, newMethod);
        }
        return root;
    }

    private static CompilationUnitSyntax AddInterfaceAttribute(CompilationUnitSyntax root, InterfaceDeclarationSyntax node, string attributeName)
    {
        if(HasAttribute(node, attributeName))
            return root;

        var newInterfaces = node.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName(attributeName)))));
        return root.ReplaceNode(node, newInterfaces);
    }

    private static CompilationUnitSyntax AddPropertyAttributes(CompilationUnitSyntax root, ClassDeclarationSyntax node, string attributeName)
    {
        foreach(var property in PropertiesWithoutAttributes(node, attributeName))
        {
            var newProperty = property.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName(attributeName)))));
            root = root.ReplaceNode(property, newProperty);
        }
        return root;
    }

    private static IEnumerable<MethodDeclarationSyntax> MethodsWithoutAttributes(InterfaceDeclarationSyntax interfaceDecl, string attributeName)
    {
        return interfaceDecl.Members.OfType<MethodDeclarationSyntax>().Where(m => !HasAttribute(m, attributeName));
    }

    private static IEnumerable<PropertyDeclarationSyntax> PropertiesWithoutAttributes(ClassDeclarationSyntax node, string attributeName)
    {
        return node.Members.OfType<PropertyDeclarationSyntax>().Where(p => !HasAttribute(p, attributeName));
    }

    private static bool HasAttribute(SyntaxNode node, string attributeName)
    {
        return node is MemberDeclarationSyntax member && member.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == attributeName);
    }

}
