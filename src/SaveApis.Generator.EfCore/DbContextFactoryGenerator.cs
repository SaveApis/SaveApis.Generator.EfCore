using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SaveApis.Generator.EfCore;

[Generator]
public class DbContextFactoryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider((node, _) => Filter(node), (syntaxContext, _) => Transform(syntaxContext));

        context.RegisterSourceOutput(provider, GenerateFactoryInterface);
        context.RegisterSourceOutput(provider, GenerateFactory);
    }

    private static bool Filter(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return false;
        }

        if (!classDeclarationSyntax.Identifier.ToString().EndsWith("DbContext"))
        {
            return false;
        }

        return classDeclarationSyntax.BaseList?.Types.Any(x => x.Type.ToString() == "BaseDbContext") == true;
    }

    private static ClassDeclarationSyntax Transform(GeneratorSyntaxContext context)
    {
        return (ClassDeclarationSyntax)context.Node;
    }

    private static void GenerateFactoryInterface(SourceProductionContext context, ClassDeclarationSyntax syntax)
    {
        var namespaceName = syntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()?.Name.ToString() ??
                            throw new InvalidOperationException("Namespace not found");

        var className = syntax.Identifier.ToString();

        var builder = new StringBuilder();
        
        builder.AppendLine($"using {namespaceName};");
        builder.AppendLine();
        builder.AppendLine($"namespace {namespaceName}.Factories;");
        builder.AppendLine();
        builder.AppendLine($"public interface I{className}Factory");
        builder.AppendLine("{");
        builder.AppendLine($"\t{className} Create();");
        builder.AppendLine("}");
        builder.AppendLine();

        context.AddSource($"{className}/I{className}Factory.g.cs", builder.ToString());
    }
    private static void GenerateFactory(SourceProductionContext context, ClassDeclarationSyntax syntax)
    {
        var namespaceName = syntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()?.Name.ToString() ??
                            throw new InvalidOperationException("Namespace not found");
        var className = syntax.Identifier.ToString();

        var builder = new StringBuilder();
        
        builder.AppendLine($"using {namespaceName};");
        builder.AppendLine("using MediatR;");
        builder.AppendLine("using Microsoft.Extensions.Configuration;");
        builder.AppendLine("using SaveApis.Common.Domains.EfCore.Infrastructure.Persistence.Sql;");
        builder.AppendLine();
        builder.AppendLine($"namespace {namespaceName}.Factories;");
        builder.AppendLine();
        builder.AppendLine($"public class {className}Factory(IConfiguration configuration, IMediator mediator) : BaseDbContextFactory<{className}>(configuration, mediator), I{className}Factory");
        builder.AppendLine("{");
        builder.AppendLine($"\tpublic {className}Factory() : this(new ConfigurationBuilder().AddInMemoryCollection().Build(), null!)");
        builder.AppendLine("\t{");
        builder.AppendLine("\t}");
        builder.AppendLine();
        builder.AppendLine($"\tpublic {className} Create()");
        builder.AppendLine("\t{");
        builder.AppendLine("\t\treturn CreateDbContext([]);");
        builder.AppendLine("\t}");
        builder.AppendLine("}");
        builder.AppendLine();

        context.AddSource($"{className}/{className}Factory.g.cs", builder.ToString());
    }
}
