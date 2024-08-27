using Apple.AppStoreConnect.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Apple.AppStoreConnect.KiotaServices;

public sealed class DefaultAccessTokenProvider(
    IJwtGenerator jwtGenerator,
    IOptions<AppleAuthenticationOptions> authenticationOptions
) : IAccessTokenProvider
{
    public async Task<string> GetAuthorizationTokenAsync(
        Uri uri, Dictionary<string, object>? additionalAuthenticationContext, CancellationToken cancellationToken
    ) => await jwtGenerator.GenerateJwtTokenAsync(cancellationToken);

    public AllowedHostsValidator AllowedHostsValidator { get; } = new(authenticationOptions.Value.AllowedHosts);
}
