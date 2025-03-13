using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SaveApis.Generator.EfCore.Helper;

public static class SyntaxHelper
{
    public static bool Filter(SyntaxNode node, string baseType)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
        {
            return false;
        }

        return classDeclaration.BaseList?.Types.Any(t => t.Type is GenericNameSyntax genericName && genericName.Identifier.ToString() == baseType)
               ?? false;
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
