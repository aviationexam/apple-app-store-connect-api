using Apple.AppStoreConnect.Client;
using Apple.AppStoreConnect.KiotaServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Net.Http;

namespace Apple.AppStoreConnect.Extensions;

public static class DependencyInjectionExtensions
{
    public const string AppStoreConnectHttpClient = "AppStoreConnect.Client";
    public const string AppStoreConnectServiceKey = "AppStoreConnect";

    public static IServiceCollection AddAppleAppStoreConnect(
        this IServiceCollection serviceCollection
    )
    {
        serviceCollection.AddHttpClient(AppStoreConnectHttpClient).AttachKiotaHandlers();

        serviceCollection.AddKeyedTransient<HttpClient>(
                AppStoreConnectServiceKey,
                (serviceProvider, _) => serviceProvider
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(AppStoreConnectHttpClient)
            )
            .AddKeyedTransient<IRequestAdapter, DefaultHttpClientRequestAdapter>(AppStoreConnectServiceKey)
            .AddKeyedSingleton<IAuthenticationProvider, DefaultAuthenticationProvider>(AppStoreConnectServiceKey)
            .AddTransient<AppStoreConnectApiClient>(serviceProvider => new AppStoreConnectApiClient(
                serviceProvider.GetRequiredKeyedService<IRequestAdapter>(AppStoreConnectServiceKey)
            ));

        return serviceCollection;
    }
}
