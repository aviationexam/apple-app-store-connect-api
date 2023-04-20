using Microsoft.CodeAnalysis;

namespace Apple.AppStoreConnect.DependencyInjection.Generator;

public record HttpClientDeclaration(
    INamedTypeSymbol Interface,
    INamedTypeSymbol Implementation
);
