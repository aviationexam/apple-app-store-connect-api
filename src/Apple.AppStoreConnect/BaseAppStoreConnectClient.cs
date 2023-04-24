using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Apple.AppStoreConnect;

public abstract class BaseAppStoreConnectClient
{
    public async Task<HttpRequestMessage> CreateHttpRequestMessageAsync(CancellationToken cancellationToken)
    {
        var httpRequestMessage = new HttpRequestMessage();

        return httpRequestMessage;
    }
}
