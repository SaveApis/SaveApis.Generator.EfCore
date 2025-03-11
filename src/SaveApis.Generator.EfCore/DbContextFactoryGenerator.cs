using System;
using System.Linq;
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

        var source = $$"""
                      using {{namespaceName}};
                      
                      namespace {{namespaceName}}.Factories;

                      public interface I{{className}}Factory
                      {
                          {{className}} Create();
                      }
                      """;

        context.AddSource($"I{className}Factory.g.cs", source);
    }

    private static void GenerateFactory(SourceProductionContext context, ClassDeclarationSyntax syntax)
    {
        var namespaceName = syntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()?.Name.ToString() ??
                            throw new InvalidOperationException("Namespace not found");
        var className = syntax.Identifier.ToString();

        var source = $$"""
                       using {{namespaceName}};
                       using Microsoft.Extensions.Configuration;
                       using SaveApis.Common.Infrastructure.Persistence.Sql.Factories;

                       namespace {{namespaceName}}.Factories;

                       public class {{className}}Factory(IConfiguration configuration) : BaseDbContextFactory<{{className}}>(configuration), I{{className}}Factory
                       {
                           public {{className}}Factory() : this(new ConfigurationBuilder().AddInMemoryCollection().Build())
                           {
                           }
                           
                           public {{className}} Create()
                           {
                                return CreateDbContext([]);
                           }
                       }
                       """;

        context.AddSource($"{className}Factory.g.cs", source);
    }
}
