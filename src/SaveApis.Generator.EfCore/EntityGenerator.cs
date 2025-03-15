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
        context.RegisterPostInitializationOutput(GenerateTrackedEntityAttribute);
        context.RegisterPostInitializationOutput(GenerateIgnoreTrackingAttribute);
        context.RegisterPostInitializationOutput(GenerateAnonymizeTrackingAttribute);

        // Entities
        var entityProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (node, _) => SyntaxHelper.FilterGenericBaseType(node, "IEntity"),
            (syntaxContext, _) => SyntaxHelper.Transform(syntaxContext)
        );
        context.RegisterSourceOutput(entityProvider, GenerateConstructor);
        context.RegisterSourceOutput(entityProvider, GenerateCreateMethod);
        context.RegisterSourceOutput(entityProvider, GenerateUpdateMethod);

        // TrackedEntities
        var trackedEntityProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (node, _) => SyntaxHelper.FilterAttribute(node, "TrackedEntity"),
            (syntaxContext, _) => SyntaxHelper.Transform(syntaxContext)
        );
        context.RegisterSourceOutput(trackedEntityProvider, GenerateTracking);
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
        builder.Append('\t').Append("private ").Append(name).Append('(').Append(string.Join(", ",
            properties.Select(p => $"{p.Type} {char.ToLower(p.Identifier.Text[0])}{p.Identifier.Text.Substring(1)}"))).AppendLine(")");
        builder.AppendLine("\t{");
        foreach (var property in properties)
        {
            builder.Append('\t').Append('\t').Append(property.Identifier.Text).Append(" = ").Append(char.ToLower(property.Identifier.Text[0]))
                .Append(property.Identifier.Text.Substring(1)).AppendLine(";");
        }
        builder.AppendLine("\t}");
        builder.AppendLine("}");
        builder.AppendLine();

        context.AddSource($"{name}/{name}.g.Constructor.cs", builder.ToString());
    }
    private static void GenerateUpdateMethod(SourceProductionContext context, ClassDeclarationSyntax syntax)
    {
        var name = syntax.Identifier.Text;
        var @namespace = syntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()!.Name.ToString();
        var usings = syntax.FirstAncestorOrSelf<CompilationUnitSyntax>()!.Usings;
        var hasTracking = syntax.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString() == "TrackedEntity"));

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
            var hasIgnoreTracking = property.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString() == "IgnoreTracking"));
            var anonymizeTracking = property.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString() == "AnonymizeTracking"));

            builder.Append('\t').Append("public ").Append(name).Append(" Update").Append(property.Identifier.Text).Append('(').Append(property.Type).Append(' ').Append(char.ToLower(property.Identifier.Text[0])).Append(property.Identifier.Text.Substring(1)).AppendLine(")");
            builder.AppendLine("\t{");
            if (hasTracking && !hasIgnoreTracking)
            {
                builder.Append("\t\tTrackChange(nameof(").Append(property.Identifier.Text).AppendLine(
                    $"), {(anonymizeTracking ? "\"***\"" : property.Identifier.Text)}.ToString(), {(anonymizeTracking ? "\"***\"" : char.ToLower(property.Identifier.Text[0]) + property.Identifier.Text.Substring(1))}.ToString());");
            }

            builder.Append("\t\t").Append(property.Identifier.Text).Append(" = ").Append(char.ToLower(property.Identifier.Text[0])).Append(property.Identifier.Text.Substring(1)).AppendLine(";");
            builder.AppendLine();
            builder.AppendLine("\t\treturn this;");
            builder.AppendLine("\t}");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        
        var source = builder.ToString();
        context.AddSource($"{name}/{name}.g.UpdateMethods.cs", source);
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
    
    // TrackedEntity
    private static void GenerateTracking(SourceProductionContext context, ClassDeclarationSyntax syntax)
    {
        var name = syntax.Identifier.Text;
        var @namespace = syntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()!.Name.ToString();
        var usings = syntax.FirstAncestorOrSelf<CompilationUnitSyntax>()!.Usings;
        
        var builder = new StringBuilder();

        builder.AppendLine("#nullable enable");
        foreach (var @using in usings)
        {
            builder.Append("using ").Append(@using.Name).AppendLine(";");
        }
        builder.AppendLine();
        builder.AppendLine($"namespace {@namespace};");
        builder.AppendLine();
        builder.AppendLine($"public partial class {name} : ITrackedEntity");
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
        builder.AppendLine("#nullable restore");
        builder.AppendLine();
        
        var source = builder.ToString();
        
        context.AddSource($"{name}/{name}.g.Tracking.cs", source);
    }

    // Infrastructure
    private static void GenerateEntityInterface(IncrementalGeneratorPostInitializationContext context)
    {
        var builder = new StringBuilder();

        builder.AppendLine("namespace SaveApis.Generator.EfCore.Infrastructure.Persistence.Sql.Entities.Interfaces;");
        builder.AppendLine();
        builder.AppendLine("public interface IEntity<TKeyType>");
        builder.AppendLine("{");
        builder.AppendLine("\tTKeyType Id { get; }");
        builder.AppendLine("}");
        builder.AppendLine();

        context.AddSource("Infrastructure/IEntity.g.cs", builder.ToString());
    }
    private static void GenerateTrackedEntityInterface(IncrementalGeneratorPostInitializationContext context)
    {
        var builder = new StringBuilder();

        builder.AppendLine("#nullable enable");
        builder.AppendLine("namespace SaveApis.Generator.EfCore.Infrastructure.Persistence.Sql.Entities.Interfaces;");
        builder.AppendLine();
        builder.AppendLine("public interface ITrackedEntity");
        builder.AppendLine("{");
        builder.AppendLine("\tIReadOnlyCollection<Tuple<string, string?, string?>> TrackedChanges { get; }");
        builder.AppendLine();
        builder.AppendLine("}");
        builder.AppendLine("#nullable restore");
        builder.AppendLine();

        context.AddSource("Infrastructure/ITrackedEntity.g.cs", builder.ToString());
    }
    private static void GenerateTrackedEntityAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        var builder = new StringBuilder();
        
        builder.AppendLine("namespace SaveApis.Generator.EfCore.Infrastructure.Persistence.Sql.Entities.Attributes;");
        builder.AppendLine();
        builder.AppendLine("[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]");
        builder.AppendLine("public sealed class TrackedEntityAttribute : Attribute;");
        builder.AppendLine();
        
        context.AddSource("Infrastructure/TrackedEntityAttribute.g.cs", builder.ToString());
    }
    private static void GenerateIgnoreTrackingAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        var builder = new StringBuilder();
        
        builder.AppendLine("namespace SaveApis.Generator.EfCore.Infrastructure.Persistence.Sql.Entities.Attributes;");
        builder.AppendLine();
        builder.AppendLine("[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]");
        builder.AppendLine("public sealed class IgnoreTrackingAttribute : Attribute;");
        builder.AppendLine();
        
        context.AddSource("Infrastructure/IgnoreTrackingAttribute.g.cs", builder.ToString());
    }
    private static void GenerateAnonymizeTrackingAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        var builder = new StringBuilder();
        
        builder.AppendLine("namespace SaveApis.Generator.EfCore.Infrastructure.Persistence.Sql.Entities.Attributes;");
        builder.AppendLine();
        builder.AppendLine("[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]");
        builder.AppendLine("public sealed class AnonymizeTrackingAttribute : Attribute;");
        builder.AppendLine();
        
        context.AddSource("Infrastructure/AnonymizeTrackingAttribute.g.cs", builder.ToString());
    }
}
