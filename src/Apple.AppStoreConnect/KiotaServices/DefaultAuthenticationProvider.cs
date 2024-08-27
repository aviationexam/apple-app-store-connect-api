using Apple.AppStoreConnect.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Apple.AppStoreConnect.KiotaServices;

public sealed class DefaultAuthenticationProvider(
    [FromKeyedServices(DependencyInjectionExtensions.AppStoreConnectServiceKey)]
    IAccessTokenProvider accessTokenProvider
) : BaseBearerTokenAuthenticationProvider(accessTokenProvider);
