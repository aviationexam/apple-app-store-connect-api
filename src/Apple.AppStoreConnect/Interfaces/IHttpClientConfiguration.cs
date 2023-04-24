using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Apple.AppStoreConnect.Interfaces;

public interface IHttpClientConfiguration
{
    Task ConfigureHttpRequestMessageAsync(
        IAppStoreConnectClient httpClient,
        HttpRequestMessage httpRequestMessage,
        CancellationToken cancellationToken
    );
}
