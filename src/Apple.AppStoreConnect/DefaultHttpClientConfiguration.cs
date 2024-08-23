using Apple.AppStoreConnect.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Apple.AppStoreConnect;

public sealed class DefaultHttpClientConfiguration(
    IJwtGenerator jwtGenerator
) : IHttpClientConfiguration
{
    public async Task ConfigureHttpRequestMessageAsync(
        IAppStoreConnectClient httpClient,
        HttpRequestMessage httpRequestMessage,
        CancellationToken cancellationToken
    )
    {
        if (httpRequestMessage.Headers is { Authorization: null } headers)
        {
            headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                await jwtGenerator.GenerateJwtTokenAsync(cancellationToken)
            );
        }
    }
}
