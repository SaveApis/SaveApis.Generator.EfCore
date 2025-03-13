using System.Linq;
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
        // Infrastructure
        context.RegisterPostInitializationOutput(GenerateEntityInterface);
        context.RegisterPostInitializationOutput(GenerateTrackedEntityInterface);

        // Entities
        var entityProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (node, _) => SyntaxHelper.Filter(node, "IEntity"),
            (syntaxContext, _) => SyntaxHelper.Transform(syntaxContext)
        );
        context.RegisterSourceOutput(entityProvider, GenerateConstructor);
        context.RegisterSourceOutput(entityProvider, GenerateCreateMethod);
        context.RegisterSourceOutput(entityProvider, GenerateUpdateMethod);

        // TrackedEntities
        var trackedEntityProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (node, _) => SyntaxHelper.Filter(node, "ITrackedEntity"),
            (syntaxContext, _) => SyntaxHelper.Transform(syntaxContext)
        );
        context.RegisterSourceOutput(trackedEntityProvider, GenerateConstructor);
        context.RegisterSourceOutput(trackedEntityProvider, GenerateCreateMethod);
        context.RegisterSourceOutput(trackedEntityProvider, GenerateTracking);
        context.RegisterSourceOutput(trackedEntityProvider, GenerateTrackingUpdateMethod);
    }

    // Entity
    private static void GenerateUpdateMethod(SourceProductionContext context, ClassDeclarationSyntax syntax)
    {
        var name = syntax.Identifier.Text;
        var @namespace = syntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()!.Name.ToString();
        var usings = syntax.FirstAncestorOrSelf<CompilationUnitSyntax>()!.Usings;
        
        var properties = syntax.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => !p.Modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)))
            .Where(p => !SyntaxHelper.IsCollectionType(p.Type))
            .Where(p => p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) && a.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) == true)
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
            builder.Append('\t').Append("public ").Append(name).Append(" Update").Append(property.Identifier.Text).Append('(').Append(property.Type).Append(' ').Append(char.ToLower(property.Identifier.Text[0])).Append(property.Identifier.Text.Substring(1)).AppendLine(")");
            builder.AppendLine("\t{");
            builder.Append("\t\t").Append("this.").Append(property.Identifier.Text).Append(" = ").Append(char.ToLower(property.Identifier.Text[0])).Append(property.Identifier.Text.Substring(1)).AppendLine(";");
            builder.AppendLine();
            builder.AppendLine("\t\treturn this;");
            builder.AppendLine("\t}");
        }

        builder.AppendLine("}");
        
        var source = builder.ToString();
        context.AddSource($"{name}/{name}.g.UpdateMethods.cs", source);
    }
    
    // TrackedEntity
    private static void GenerateTracking(SourceProductionContext context, ClassDeclarationSyntax syntax)
    {
        var name = syntax.Identifier.Text;
        var @namespace = syntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()!.Name.ToString();
        var usings = syntax.FirstAncestorOrSelf<CompilationUnitSyntax>()!.Usings;
        
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
        builder.AppendLine("\tprivate readonly ICollection<Tuple<string, string?, string?>> _trackedChanges = [];");
        builder.AppendLine();
        builder.AppendLine("\tpublic IReadOnlyCollection<Tuple<string, string?, string?>> TrackedChanges => _trackedChanges.ToList();");
        builder.AppendLine();
        builder.AppendLine("\tprivate void TrackChange(string propertyName, string? oldValue, string? newValue)");
        builder.AppendLine("\t{");
        builder.AppendLine("\t\t_trackedChanges.Add(new Tuple<string, string?, string?>(propertyName, oldValue, newValue));");
        builder.AppendLine("\t}");
        builder.AppendLine("}");
        
        var source = builder.ToString();
        
        context.AddSource($"{name}/{name}.g.Tracking.cs", source);
    }
    private static void GenerateTrackingUpdateMethod(SourceProductionContext context, ClassDeclarationSyntax syntax)
    {
        var name = syntax.Identifier.Text;
        var @namespace = syntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()!.Name.ToString();
        var usings = syntax.FirstAncestorOrSelf<CompilationUnitSyntax>()!.Usings;
        
        var properties = syntax.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => !p.Modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)))
            .Where(p => !SyntaxHelper.IsCollectionType(p.Type))
            .Where(p => p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) && a.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) == true)
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
            builder.Append('\t').Append("public ").Append(name).Append(" Update").Append(property.Identifier.Text).Append('(').Append(property.Type).Append(' ').Append(char.ToLower(property.Identifier.Text[0])).Append(property.Identifier.Text.Substring(1)).AppendLine(")");
            builder.AppendLine("\t{");
            builder.AppendLine("\t\tif (this.").Append(property.Identifier.Text).Append(" != ").Append(char.ToLower(property.Identifier.Text[0])).Append(property.Identifier.Text.Substring(1)).AppendLine(")");
            builder.AppendLine("\t\t{");
            builder.AppendLine("\t\t\tTrackChange(nameof(").Append(property.Identifier.Text).Append($"), {property.Identifier.Text}, ").Append(char.ToLower(property.Identifier.Text[0])).Append(property.Identifier.Text.Substring(1)).AppendLine(");");
            builder.AppendLine("\t\t}");
            builder.Append("\t\t").Append("this.").Append(property.Identifier.Text).Append(" = ").Append(char.ToLower(property.Identifier.Text[0])).Append(property.Identifier.Text.Substring(1)).AppendLine(";");
            builder.AppendLine();
            builder.AppendLine("\t\treturn this;");
            builder.AppendLine("\t}");
        }
        
        builder.AppendLine("}");
        
        var source = builder.ToString();
        
        context.AddSource($"{name}/{name}.g.TrackingUpdateMethod.cs", source);
    }
    
    // Global
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

        var source = $$"""
                       {{string.Join("\n", usings.Select(u => $"using {u.Name};"))}}

                       namespace {{@namespace}};

                       public partial class {{name}}
                       {
                           private {{name}}({{string.Join(", ", properties.Select(p => $"{p.Type} {char.ToLower(p.Identifier.Text[0])}{p.Identifier.Text.Substring(1)}"))}})
                           {
                               {{string.Join("\n\t\t", properties.Select(p => $"this.{p.Identifier.Text} = {char.ToLower(p.Identifier.Text[0])}{p.Identifier.Text.Substring(1)};"))}}
                           }
                       }
                       """;
        context.AddSource($"{name}/{name}.g.Constructor.cs", source);
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
            builder.Append("using ").Append(@using.Name).AppendLine(";");
        }
        builder.AppendLine();
        builder.AppendLine($"namespace {@namespace};");
        builder.AppendLine();
        builder.AppendLine($"public partial class {name}");
        builder.AppendLine("{");

        builder.Append('\t').Append("public static ").Append(name).Append(" Create").Append('(').Append(string.Join(", ",
            properties.Select(p => $"{p.Type} {char.ToLower(p.Identifier.Text[0])}{p.Identifier.Text.Substring(1)}"))).AppendLine(")");
        builder.AppendLine("\t{");
        builder.Append("\t\t").Append("return new ").Append(name).Append('(').Append(string.Join(", ",
            properties.Select(p => $"{char.ToLower(p.Identifier.Text[0])}{p.Identifier.Text.Substring(1)}"))).AppendLine(");");
        builder.AppendLine("\t}");
        builder.AppendLine("}");
        
        var source = builder.ToString();
        context.AddSource($"{name}/{name}.g.CreateMethod.cs", source);
    }
    
    
    // Infrastructure
    private static void GenerateEntityInterface(IncrementalGeneratorPostInitializationContext context)
    {
        const string source = """
                              namespace SaveApis.Generator.EfCore.Infrastructure.Persistence.Sql.Entities;

                              public interface IEntity<TKeyType>
                              {
                                 TKeyType Id { get; }
                              }
                              """;

        context.AddSource("Infrastructure/IEntity.g.cs", source);
    }
    private static void GenerateTrackedEntityInterface(IncrementalGeneratorPostInitializationContext context)
    {
        const string source = """
                              namespace SaveApis.Generator.EfCore.Infrastructure.Persistence.Sql.Entities;

                              public interface ITrackedEntity<TKeyType> : IEntity<TKeyType>;
                              """;

        context.AddSource("Infrastructure/ITrackedEntity.g.cs", source);
    }
}
