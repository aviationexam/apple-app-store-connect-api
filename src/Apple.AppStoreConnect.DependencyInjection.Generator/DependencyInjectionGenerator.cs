using H.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Apple.AppStoreConnect.DependencyInjection.Generator;

[Generator]
public class DependencyInjectionGenerator : IIncrementalGenerator
{
    private const string AppStoreConnectClient = "Apple.AppStoreConnect.Interfaces.IAppStoreConnectClient";
    public const string Id = "DIG";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }

        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "AppStoreConnectDependencyInjectionAttribute.g.cs",
            SourceText.From(SourceGenerationHelper.AppStoreConnectDependencyInjectionAttribute, Encoding.UTF8)
        ));

        var source = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: GetSemanticTargetForGeneration
            )
            .Where(static m => m is not null)
            .Select(static (m, _) => m!)
            .Combine(context.CompilationProvider
                .SelectMany(GetHttpClients)
                .Collect()
            )
            .SelectAndReportExceptions(GetSourceCode, context, Id);

        context.RegisterSourceOutput(source, static (context, file) =>
        {
            context.AddSource(
                hintName: file.fileName,
                sourceText: file.sourceText
            );
        });
    }

    private static (string fileName, SourceText sourceText) GetSourceCode(
        (
            ClassDeclarationSyntax extensionClassDeclarationSyntax,
            ImmutableArray<HttpClientDeclaration> interfaceDeclarationsSyntax
            ) item,
        CancellationToken cancellationToken
    )
    {
        var syntaxTree = item.extensionClassDeclarationSyntax.SyntaxTree;

        var tree = syntaxTree
            .WithRootAndOptions(syntaxTree
                    .GetCompilationUnitRoot()
                    .WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>())
                    .WithUsings(SyntaxFactory.List<UsingDirectiveSyntax>())
                    .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>())
                    .NormalizeWhitespace(), syntaxTree.Options
            );

        var classUnit = tree.GetCompilationUnitRoot(cancellationToken).ToFullString();

        return (
            fileName: item.extensionClassDeclarationSyntax.Identifier.Text + ".g.cs",
            sourceText: SourceText.From(
                classUnit,
                Encoding.Default
            )
        );
    }

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(
        GeneratorSyntaxContext context, CancellationToken cancellationToken
    )
    {
        // we know the node is a EnumDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        var classDeclarationSyntax = (ClassDeclarationSyntax) context.Node;

        // loop through all the attributes on the method
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol
                    attributeSymbol)
                {
                    // weird, we couldn't get the symbol, ignore it
                    continue;
                }

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName == SourceGenerationHelper.AppStoreConnectDependencyInjectionAttributeFullname)
                {
                    return classDeclarationSyntax;
                }
            }
        }

        // we didn't find the attribute we were looking for
        return null;
    }

    private static IEnumerable<HttpClientDeclaration> GetHttpClients(
        Compilation compilation,
        CancellationToken cancellationToken
    )
    {
        var appStoreConnectClientMetadataName = compilation.GetTypeByMetadataName(AppStoreConnectClient);

        if (appStoreConnectClientMetadataName is null)
        {
            yield break;
        }

        var assemblySymbol = appStoreConnectClientMetadataName.ContainingAssembly;

        foreach (var namedTypeSymbol in GetNamedTypeSymbols(assemblySymbol))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (
                namedTypeSymbol.TypeKind is TypeKind.Class
                && namedTypeSymbol.Interfaces.SingleOrDefault(
                    x => x.Interfaces.Any(
                        i => SymbolEqualityComparer.Default.Equals(i, appStoreConnectClientMetadataName)
                    )
                ) is { } implementationInterface
            )
            {
                yield return new HttpClientDeclaration(
                    Interface: implementationInterface,
                    Implementation: namedTypeSymbol
                );
            }
        }
    }

    public static IEnumerable<INamedTypeSymbol> GetNamedTypeSymbols(IAssemblySymbol assemblySymbol)
    {
        var stack = new Stack<INamespaceSymbol>();
        stack.Push(assemblySymbol.GlobalNamespace);

        while (stack.Count > 0)
        {
            var namespaceSymbol = stack.Pop();

            foreach (var memberSymbol in namespaceSymbol.GetMembers())
            {
                switch (memberSymbol)
                {
                    case INamespaceSymbol memberAsNamespace:
                        stack.Push(memberAsNamespace);
                        break;
                    case INamedTypeSymbol memberAsNamedTypeSymbol:
                        yield return memberAsNamedTypeSymbol;
                        break;
                }
            }
        }
    }
}