using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        var extensionClassDeclarationSyntax = item.extensionClassDeclarationSyntax;
        var className = extensionClassDeclarationSyntax.Identifier.Text;
        var syntaxTree = extensionClassDeclarationSyntax.SyntaxTree;

        var compilationUnitSyntax = syntaxTree
            .GetCompilationUnitRoot()
            .WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>())
            .WithUsings(SyntaxFactory.List(new[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.Extensions.DependencyInjection")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic"))
            }))
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                SyntaxFactory.FileScopedNamespaceDeclaration(
                        SyntaxFactory.IdentifierName(GetNamespaceFrom(extensionClassDeclarationSyntax))
                    )
                    .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.ClassDeclaration(className)
                            .WithTypeParameterList(extensionClassDeclarationSyntax.TypeParameterList)
                            .WithModifiers(extensionClassDeclarationSyntax.Modifiers)
                            .WithMembers(SyntaxFactory.List(GetClassMembers(item.interfaceDeclarationsSyntax)))
                    ))
            ))
            .NormalizeWhitespace();

        compilationUnitSyntax = compilationUnitSyntax.ReplaceNodes(
            compilationUnitSyntax.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .Where(x => x.Type.ToFullString() == DefaultHttpClientBuilderClass),
            (_, node) => node.WithTrailingTrivia(
                SyntaxFactory.CarriageReturnLineFeed,
                SyntaxFactory.Whitespace("            ")
            )
        );

        var tree = syntaxTree.WithRootAndOptions(compilationUnitSyntax, syntaxTree.Options);

        var classUnit = tree.GetCompilationUnitRoot(cancellationToken).ToFullString();

        return (
            fileName: $"{className}.g.cs",
            sourceText: SourceText.From(
                classUnit,
                Encoding.Default
            )
        );
    }

    private const string DefaultHttpClientBuilderClass = "DefaultHttpClientBuilder";
    private const string ServiceCollectionParameter = "serviceCollection";
    private const string HttpClientConfigurationsParameter = "httpClientConfigurations";

    private static IEnumerable<MemberDeclarationSyntax> GetClassMembers(
        ImmutableArray<HttpClientDeclaration> interfaceDeclarationsSyntax
    )
    {
        yield return SyntaxFactory.MethodDeclaration(SyntaxFactory.IdentifierName("void"), "GetHttpClientDeclaration")
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                SyntaxFactory.Token(SyntaxKind.PartialKeyword)
            ))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
                new[]
                {
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(ServiceCollectionParameter))
                        .WithType(SyntaxFactory.IdentifierName("IServiceCollection")),
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(HttpClientConfigurationsParameter))
                        .WithType(SyntaxFactory.IdentifierName("IReadOnlyDictionary<Type, Action<IHttpClientBuilder>>"))
                }
            )))
            .WithBody(SyntaxFactory.Block(SyntaxFactory.List(GetMethodMembers(interfaceDeclarationsSyntax))));
    }

    private static IEnumerable<StatementSyntax> GetMethodMembers(
        ImmutableArray<HttpClientDeclaration> interfaceDeclarationsSyntax
    )
    {
        const string hasHttpClientGlobalConfigurationVariable = "hasHttpClientConfiguration";
        const string httpClientGlobalConfigurationVariable = "httpClientConfiguration";

        yield return SyntaxFactory.LocalDeclarationStatement(SyntaxFactory
            .VariableDeclaration(SyntaxFactory.ParseTypeName("var"))
            .AddVariables(
                SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(hasHttpClientGlobalConfigurationVariable))
                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(HttpClientConfigurationsParameter),
                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                SyntaxFactory.IdentifierName("TryGetValue")
                            ),
                            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.TypeOfExpression(SyntaxFactory.IdentifierName(AppStoreConnectClient))
                                ),
                                SyntaxFactory.Argument(SyntaxFactory.DeclarationExpression(
                                    SyntaxFactory.IdentifierName("var"),
                                    SyntaxFactory.SingleVariableDesignation(
                                        SyntaxFactory.Identifier(httpClientGlobalConfigurationVariable))
                                )).WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword)),
                            }))
                        )
                    ))
            )
        );

        foreach (var interfaceDeclarationSyntax in interfaceDeclarationsSyntax)
        {
            var interfaceName = interfaceDeclarationSyntax.Interface.Name;
            var implementationName = interfaceDeclarationSyntax.Implementation.Name;
            var httpClientBuilderVariable = $"httpClientBuilder{interfaceName}";
            var httpClientConfigurationVariable = $"httpClientConfiguration{interfaceName}";

            var httpClientBuilderExpression = SyntaxFactory
                .ObjectCreationExpression(SyntaxFactory.IdentifierName(DefaultHttpClientBuilderClass))
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(ServiceCollectionParameter)),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(
                            $"Apple.AppStoreConnect.{interfaceName}"
                        )
                    ))
                );

            var httpClientBuilderWithAddTypedClientInvocationSyntax = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    httpClientBuilderExpression,
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("AddTypedClient"),
                        SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                            new[]
                            {
                                SyntaxFactory.ParseTypeName(interfaceName),
                                SyntaxFactory.ParseTypeName(implementationName),
                            }
                        ))
                    )
                )
            );

            yield return SyntaxFactory.LocalDeclarationStatement(SyntaxFactory
                .VariableDeclaration(SyntaxFactory.ParseTypeName("var"))
                .AddVariables(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(httpClientBuilderVariable))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                            httpClientBuilderWithAddTypedClientInvocationSyntax
                        ))
                )
            );

            yield return SyntaxFactory.IfStatement(
                SyntaxFactory.IdentifierName(hasHttpClientGlobalConfigurationVariable),
                SyntaxFactory.Block(SyntaxFactory.SingletonList(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                                SyntaxFactory.IdentifierName(httpClientGlobalConfigurationVariable)
                            )
                            .AddArgumentListArguments(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName(httpClientBuilderVariable)
                                )
                            )
                    )
                ))
            );

            yield return SyntaxFactory.IfStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(HttpClientConfigurationsParameter),
                        SyntaxFactory.Token(SyntaxKind.DotToken),
                        SyntaxFactory.IdentifierName("TryGetValue")
                    ),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(
                            SyntaxFactory.TypeOfExpression(SyntaxFactory.IdentifierName(interfaceName))
                        ),
                        SyntaxFactory.Argument(SyntaxFactory.DeclarationExpression(
                            SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.SingleVariableDesignation(
                                SyntaxFactory.Identifier(httpClientConfigurationVariable))
                        )).WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword)),
                    }))
                ),
                SyntaxFactory.Block(SyntaxFactory.SingletonList(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                                SyntaxFactory.IdentifierName(httpClientConfigurationVariable)
                            )
                            .AddArgumentListArguments(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName(httpClientBuilderVariable)
                                )
                            )
                    )
                ))
            );
        }
    }

    private static string GetNamespaceFrom(SyntaxNode s) => s.Parent switch
    {
        FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclarationSyntax =>
            fileScopedNamespaceDeclarationSyntax.Name.ToString(),
        NamespaceDeclarationSyntax namespaceDeclarationSyntax => namespaceDeclarationSyntax.Name.ToString(),
        null => string.Empty, // or whatever you want to do
        _ => GetNamespaceFrom(s.Parent)
    };

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
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                {
                    // weird, we couldn't get the symbol, ignore it
                    continue;
                }

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName is SourceGenerationHelper.AppStoreConnectDependencyInjectionAttributeFullname)
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
