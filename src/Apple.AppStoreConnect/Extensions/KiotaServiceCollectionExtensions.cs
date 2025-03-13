using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Net.Http;

namespace Apple.AppStoreConnect.Extensions;

public static class KiotaServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Kiota handlers to the http client builder.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    /// <remarks>
    /// The order in which the handlers are added is important, as it defines the order in which they will be executed.
    /// </remarks>
    public static IHttpClientBuilder AttachKiotaHandlers(this IHttpClientBuilder builder)
    {
        // Dynamically load the Kiota handlers from the Client Factory
        var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerActivatableTypes();
        // And attach them to the http client builder
        foreach (var handlerType in kiotaHandlers)
        {
            builder.Services.Add(new ServiceDescriptor(
                serviceType: handlerType,
                serviceKey: DependencyInjectionExtensions.AppStoreConnectServiceKey,
                implementationType: handlerType,
                lifetime: ServiceLifetime.Transient
            ));

            builder.AddHttpMessageHandler(sp => (DelegatingHandler) sp.GetRequiredKeyedService(
                handlerType,
                serviceKey: DependencyInjectionExtensions.AppStoreConnectServiceKey
            ));
        }

        return builder;
    }
}
