using Apple.AppStoreConnect.Client;
using Apple.AppStoreConnect.Interfaces;
using Apple.AppStoreConnect.KiotaServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Net.Http;

namespace Apple.AppStoreConnect.Extensions;

public static class DependencyInjectionExtensions
{
    public const string AppStoreConnectHttpClient = "AppStoreConnect.Client";
    public const string AppStoreConnectServiceKey = "AppStoreConnect";

    public static IServiceCollection AddAppleAppStoreConnect(
        this IServiceCollection serviceCollection,
        Action<OptionsBuilder<AppleAuthenticationOptions>> optionsBuilder
    )
    {
        serviceCollection.AddHttpClient(AppStoreConnectHttpClient).AttachKiotaHandlers();

        optionsBuilder(serviceCollection
            .AddOptions<AppleAuthenticationOptions>()
        );

        serviceCollection.TryAddEnumerable(ServiceDescriptor
            .Singleton<IPostConfigureOptions<AppleAuthenticationOptions>, AppleAuthenticationPostConfigure>()
        );
        serviceCollection.TryAddEnumerable(ServiceDescriptor
            .Singleton<IValidateOptions<AppleAuthenticationOptions>, AppleAuthenticationOptionsValidate>()
        );

        serviceCollection.TryAddSingleton<IJwtGenerator, DefaultJwtGenerator>();

        serviceCollection.AddKeyedTransient<HttpClient>(
            AppStoreConnectServiceKey,
            (serviceProvider, _) => serviceProvider
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient(AppStoreConnectHttpClient)
        );

        serviceCollection.TryAddKeyedTransient<IRequestAdapter, DefaultHttpClientRequestAdapter>(AppStoreConnectServiceKey);
        serviceCollection.TryAddKeyedSingleton<IAuthenticationProvider, DefaultAuthenticationProvider>(AppStoreConnectServiceKey);

        serviceCollection.AddTransient<AppStoreConnectApiClient>(serviceProvider => new AppStoreConnectApiClient(
                serviceProvider.GetRequiredKeyedService<IRequestAdapter>(AppStoreConnectServiceKey)
            ));

        return serviceCollection;
    }
}
