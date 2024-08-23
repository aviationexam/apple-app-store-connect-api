using Apple.AppStoreConnect.Interfaces;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Apple.AppStoreConnect;

public abstract class BaseAppStoreConnectClient(
    IHttpClientConfiguration httpClientConfiguration
)
    : IAppStoreConnectClient
{
    public async Task<HttpRequestMessage> CreateHttpRequestMessageAsync(CancellationToken cancellationToken)
    {
        var httpRequestMessage = new HttpRequestMessage();

        await httpClientConfiguration.ConfigureHttpRequestMessageAsync(
            this,
            httpRequestMessage,
            cancellationToken
        );

        return httpRequestMessage;
    }
}
