﻿using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SaveApis.Generator.EfCore.Helper;

namespace SaveApis.Generator.EfCore;

[Generator]
public class EntityGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Entities
        var entityProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (node, _) => SyntaxHelper.FilterBaseType(node, "IEntity"),
            (syntaxContext, _) => SyntaxHelper.Transform(syntaxContext)
        );
        context.RegisterSourceOutput(entityProvider, GenerateConstructor);
        context.RegisterSourceOutput(entityProvider, GenerateCreateMethod);
        context.RegisterSourceOutput(entityProvider, GenerateUpdateMethod);
    }

    // Entity
    private static void GenerateConstructor(SourceProductionContext context, ClassDeclarationSyntax syntax)
    {
        var name = syntax.Identifier.Text;
        var @namespace = syntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()!.Name.ToString();
        var usings = syntax.FirstAncestorOrSelf<CompilationUnitSyntax>()!.Usings;

        var properties = syntax.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => !p.Modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)))
            .Where(p => !SyntaxHelper.IsCollectionType(p.Type))
            .ToList();
        
        var builder = new StringBuilder();
        
        foreach (var @using in usings)
        {
            builder.Append("using ").Append(@using.Name).AppendLine(";");
        }
        builder.AppendLine();
        builder.AppendLine($"namespace {@namespace};");
        builder.AppendLine();
        builder.AppendLine($"public partial class {name}");
        builder.AppendLine("{");
        builder.AppendLine($"\tprivate {name}({string.Join(", ", properties.Select(p => $"{p.Type} {(SyntaxHelper.IsKeyWord(p) ? '@' : "")}{char.ToLower(p.Identifier.Text[0])}{p.Identifier.Text.Substring(1)}"))})");
        builder.AppendLine("\t{");
        foreach (var property in properties)
        {
            builder.AppendLine($"\t\t{property.Identifier.Text} = {(SyntaxHelper.IsKeyWord(property) ? '@' : "")}{char.ToLower(property.Identifier.Text[0])}{property.Identifier.Text.Substring(1)};");
        }
        builder.AppendLine("\t}");
        builder.AppendLine("}");
        builder.AppendLine();

        context.AddSource($"{name}/{name}.g.Constructor.cs", builder.ToString());
    }
    private static void GenerateCreateMethod(SourceProductionContext context, ClassDeclarationSyntax syntax)
    {
        var name = syntax.Identifier.Text;
        var @namespace = syntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()!.Name.ToString();
        var usings = syntax.FirstAncestorOrSelf<CompilationUnitSyntax>()!.Usings;

        var properties = syntax.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => !p.Modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)))
            .Where(p => !SyntaxHelper.IsCollectionType(p.Type))
            .ToList();

        var builder = new StringBuilder();

        foreach (var @using in usings)
        {
            builder.AppendLine($"using {@using.Name};");
        }
        builder.AppendLine();
        builder.AppendLine($"namespace {@namespace};");
        builder.AppendLine();
        builder.AppendLine($"public partial class {name}");
        builder.AppendLine("{");
        builder.AppendLine(
            $"\tpublic static {name} Create({string.Join(", ", properties.Select(p => $"{p.Type} {(SyntaxHelper.IsKeyWord(p) ? '@' : "")}{char.ToLower(p.Identifier.Text[0])}{p.Identifier.Text.Substring(1)}"))})");
        builder.AppendLine("\t{");
        builder.AppendLine(
            $"\t\treturn new {name}({string.Join(", ", properties.Select(p => $"{(SyntaxHelper.IsKeyWord(p) ? '@' : "")}{char.ToLower(p.Identifier.Text[0])}{p.Identifier.Text.Substring(1)}"))});");
        builder.AppendLine("\t}");
        builder.AppendLine("}");
        builder.AppendLine();
        
        var source = builder.ToString();
        context.AddSource($"{name}/{name}.g.CreateMethod.cs", source);
    }
    private static void GenerateUpdateMethod(SourceProductionContext context, ClassDeclarationSyntax syntax)
    {
        var name = syntax.Identifier.Text;
        var @namespace = syntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()!.Name.ToString();
        var usings = syntax.FirstAncestorOrSelf<CompilationUnitSyntax>()!.Usings;

        var properties = syntax.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => !p.Modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)))
            .Where(p => !SyntaxHelper.IsCollectionType(p.Type))
            .Where(p => p.AccessorList?.Accessors.Any(a =>
                a.IsKind(SyntaxKind.SetAccessorDeclaration) && a.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) == true)
            .ToList();

        var builder = new StringBuilder();

        foreach (var @using in usings)
        {
            builder.Append("using ").Append(@using.Name).AppendLine(";");
        }
        builder.AppendLine();
        builder.AppendLine($"namespace {@namespace};");
        builder.AppendLine();
        builder.AppendLine($"public partial class {name}");
        builder.AppendLine("{");

        foreach (var property in properties)
        {
            builder.AppendLine($"\tpublic {name} Update{property.Identifier.Text}({property.Type} {(SyntaxHelper.IsKeyWord(property) ? '@' : "")}{char.ToLower(property.Identifier.Text[0])}{property.Identifier.Text.Substring(1)})");
            builder.AppendLine("\t{");
            builder.AppendLine($"\t\t{property.Identifier.Text} = {(SyntaxHelper.IsKeyWord(property) ? '@' : "")}{char.ToLower(property.Identifier.Text[0])}{property.Identifier.Text.Substring(1)};");
            builder.AppendLine();
            builder.AppendLine("\t\treturn this;");
            builder.AppendLine("\t}");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        builder.AppendLine();
        
        var source = builder.ToString();
        context.AddSource($"{name}/{name}.g.UpdateMethods.cs", source);
    }
}
