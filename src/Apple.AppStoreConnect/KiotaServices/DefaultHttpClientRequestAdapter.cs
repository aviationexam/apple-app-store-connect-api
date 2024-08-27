using Apple.AppStoreConnect.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Net.Http;

namespace Apple.AppStoreConnect.KiotaServices;

public class DefaultHttpClientRequestAdapter(
    [FromKeyedServices(DependencyInjectionExtensions.AppStoreConnectServiceKey)]
    IAuthenticationProvider authenticationProvider,
    [FromKeyedServices(DependencyInjectionExtensions.AppStoreConnectServiceKey)]
    IParseNodeFactory? parseNodeFactory = null,
    [FromKeyedServices(DependencyInjectionExtensions.AppStoreConnectServiceKey)]
    ISerializationWriterFactory? serializationWriterFactory = null,
    [FromKeyedServices(DependencyInjectionExtensions.AppStoreConnectServiceKey)]
    HttpClient? httpClient = null,
    [FromKeyedServices(DependencyInjectionExtensions.AppStoreConnectServiceKey)]
    ObservabilityOptions? observabilityOptions = null
) : HttpClientRequestAdapter(
    authenticationProvider, parseNodeFactory, serializationWriterFactory, httpClient, observabilityOptions
);
