using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SaveApis.Generator.EfCore.Helper;

public static class SyntaxHelper
{
    public static bool FilterBaseType(SyntaxNode node, string baseType)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
        {
            return false;
        }
        
        var hasBaseType = classDeclaration.BaseList?.Types.Any(it => it.Type.ToString() == baseType) ?? false;
        return hasBaseType;
    }

    public static bool FilterGenericBaseType(SyntaxNode node, string baseType)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
        {
            return false;
        }

        var hasGenericBaseType = classDeclaration.BaseList?.Types.Any(t => t.Type is GenericNameSyntax genericName && genericName.Identifier.ToString() == baseType)
                                ?? false;
        return hasGenericBaseType;
    }
    
    public static bool FilterAttribute(SyntaxNode node, string attribute)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
        {
            return false;
        }

        var hasAttribute = classDeclaration.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString() == attribute));
        return hasAttribute;
    }

    public static ClassDeclarationSyntax Transform(GeneratorSyntaxContext context)
    {
        return (ClassDeclarationSyntax)context.Node;
    }

    public static bool IsCollectionType(TypeSyntax typeSyntax)
    {
        var collectionTypes = new[] { "IEnumerable", "ICollection", "IList", "List", "HashSet", "Dictionary" };
        var typeName = typeSyntax.ToString();

        return collectionTypes.Any(ct => typeName.StartsWith(ct, StringComparison.Ordinal));
    }
}
