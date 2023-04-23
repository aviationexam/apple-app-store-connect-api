using Apple.AppStoreConnect.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Apple.AppStoreConnect.DependencyInjection;

[AppStoreConnectDependencyInjection]
public static partial class AppStoreConnectExtensions
{
    public static IServiceCollection AddAppleAppStoreConnect(
        this IServiceCollection serviceCollection,
        Action<OptionsBuilder<AppleAuthenticationOptions>> optionsBuilder
    ) => serviceCollection
        .AddAppleAppStoreConnect(
            optionsBuilder,
            ImmutableDictionary.Create<Type, Action<IHttpClientBuilder>>()
        );

    public static IServiceCollection AddAppleAppStoreConnect(
        this IServiceCollection serviceCollection,
        Action<OptionsBuilder<AppleAuthenticationOptions>> optionsBuilder,
        IReadOnlyDictionary<Type, Action<IHttpClientBuilder>> httpClientConfigurations
    )
    {
        serviceCollection
            .AddHttpClient()
            .AddSingleton<IJwtGenerator, DefaultJwtGenerator>();

        optionsBuilder(serviceCollection
            .AddOptions<AppleAuthenticationOptions>()
        );

        serviceCollection.TryAddEnumerable(ServiceDescriptor
            .Singleton<IPostConfigureOptions<AppleAuthenticationOptions>, AppleAuthenticationPostConfigure>()
        );
        serviceCollection.TryAddEnumerable(ServiceDescriptor
            .Singleton<IValidateOptions<AppleAuthenticationOptions>, AppleAuthenticationOptionsValidate>()
        );

        GetHttpClientDeclaration(serviceCollection, httpClientConfigurations);

        return serviceCollection;
    }

    static partial void GetHttpClientDeclaration(
        IServiceCollection serviceCollection,
        IReadOnlyDictionary<Type, Action<IHttpClientBuilder>> httpClientConfigurations
    );
}
