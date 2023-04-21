using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Apple.AppStoreConnect.DependencyInjection;

[AppStoreConnectDependencyInjection]
public static partial class AppStoreConnectExtensions
{
    public static IServiceCollection AddAppleAppStoreConnect(
        this IServiceCollection serviceCollection
    ) => serviceCollection.AddAppleAppStoreConnect(ImmutableDictionary.Create<Type, Action<IHttpClientBuilder>>());

    public static IServiceCollection AddAppleAppStoreConnect(
        this IServiceCollection serviceCollection,
        IReadOnlyDictionary<Type, Action<IHttpClientBuilder>> httpClientConfigurations
    )
    {
        serviceCollection
            .AddHttpClient();

        GetHttpClientDeclaration(serviceCollection, httpClientConfigurations);

        return serviceCollection;
    }

    static partial void GetHttpClientDeclaration(
        IServiceCollection serviceCollection,
        IReadOnlyDictionary<Type, Action<IHttpClientBuilder>> httpClientConfigurations
    );
}
