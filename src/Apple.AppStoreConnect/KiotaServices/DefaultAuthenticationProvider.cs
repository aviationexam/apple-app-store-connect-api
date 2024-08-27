using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Apple.AppStoreConnect.KiotaServices;


public sealed class DefaultAuthenticationProvider : IAuthenticationProvider
{
    public async Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext,
        CancellationToken cancellationToken
    )
    {
    }
}
