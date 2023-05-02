using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
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

    IEnumerable<JsonConverter> GetJsonConverters();
}
