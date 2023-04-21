using Microsoft.Extensions.DependencyInjection;

namespace Apple.AppStoreConnect.DependencyInjection;

/// <summary>
/// Copy of internal <see cref="Microsoft.Extensions.DependencyInjection.DefaultHttpClientBuilder"/>
/// </summary>
internal sealed record DefaultHttpClientBuilder(
    IServiceCollection Services, string Name
) : IHttpClientBuilder;
