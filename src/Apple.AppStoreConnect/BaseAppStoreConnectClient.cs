using Apple.AppStoreConnect.Interfaces;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Apple.AppStoreConnect;

public abstract class BaseAppStoreConnectClient
{
    private readonly IHttpClientConfiguration _httpClientConfiguration;

    protected BaseAppStoreConnectClient(IHttpClientConfiguration httpClientConfiguration)
    {
        _httpClientConfiguration = httpClientConfiguration;
    }

    public async Task<HttpRequestMessage> CreateHttpRequestMessageAsync(CancellationToken cancellationToken)
    {
        var httpRequestMessage = new HttpRequestMessage();

        await _httpClientConfiguration.ConfigureHttpRequestMessageAsync(httpRequestMessage, cancellationToken);

        return httpRequestMessage;
    }
}
