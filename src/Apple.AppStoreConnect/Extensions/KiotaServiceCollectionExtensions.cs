using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Net.Http;

namespace Apple.AppStoreConnect.Extensions;

public static class KiotaServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Kiota handlers to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add the services to</param>
    /// <returns><see cref="AttachKiotaHandlers"/> as per convention</returns>
    /// <remarks>The handlers are added to the http client by the <see cref="IHttpClientBuilder"/> call, which requires them to be pre-registered in DI</remarks>
    private static IServiceCollection AddKiotaHandlers(this IServiceCollection services)
    {
        // Dynamically load the Kiota handlers from the Client Factory
        var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerTypes();
        // And register them in the DI container
        foreach (var handlerType in kiotaHandlers)
        {
            services.Add(new ServiceDescriptor(
                serviceType: handlerType,
                serviceKey: DependencyInjectionExtensions.AppStoreConnectServiceKey,
                implementationType: handlerType,
                lifetime: ServiceLifetime.Transient
            ));
        }

        return services;
    }

    /// <summary>
    /// Adds the Kiota handlers to the http client builder.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    /// <remarks>
    /// Requires the handlers to be registered in DI by <see cref="AddKiotaHandlers(IServiceCollection)"/>.
    /// The order in which the handlers are added is important, as it defines the order in which they will be executed.
    /// </remarks>
    public static IHttpClientBuilder AttachKiotaHandlers(this IHttpClientBuilder builder)
    {
        builder.Services.AddKiotaHandlers();

        // Dynamically load the Kiota handlers from the Client Factory
        var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerTypes();
        // And attach them to the http client builder
        foreach (var handler in kiotaHandlers)
        {
            builder.AddHttpMessageHandler(sp => (DelegatingHandler) sp.GetRequiredKeyedService(
                handler,
                serviceKey: DependencyInjectionExtensions.AppStoreConnectServiceKey
            ));
        }

        return builder;
    }
}
