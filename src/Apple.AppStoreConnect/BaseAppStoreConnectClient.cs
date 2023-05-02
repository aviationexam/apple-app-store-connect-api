using Apple.AppStoreConnect.Interfaces;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Apple.AppStoreConnect;

public abstract class BaseAppStoreConnectClient : IAppStoreConnectClient
{
    private readonly IHttpClientConfiguration _httpClientConfiguration;

    protected BaseAppStoreConnectClient(
        IHttpClientConfiguration httpClientConfiguration
    )
    {
        _httpClientConfiguration = httpClientConfiguration;

        AddJsonConverters();
    }

    /// <summary>
    /// TODO implement me through source-generator
    /// see partial UpdateJsonSerializerSettings(JsonSerializerOptions) of HttpClients
    /// </summary>
    private void AddJsonConverters()
    {
        var jsonSerializerSettingsPropertyInfo = GetType().GetProperty(
            "JsonSerializerSettings", BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        var jsonSerializerOptions = (JsonSerializerOptions) jsonSerializerSettingsPropertyInfo.GetValue(this)!;

        AddJsonConverters(jsonSerializerOptions);
    }

    private void AddJsonConverters(JsonSerializerOptions jsonSerializerOptions)
    {
        foreach (var converter in _httpClientConfiguration.GetJsonConverters())
        {
            jsonSerializerOptions.Converters.Add(converter);
        }
    }

    public async Task<HttpRequestMessage> CreateHttpRequestMessageAsync(CancellationToken cancellationToken)
    {
        var httpRequestMessage = new HttpRequestMessage();

        await _httpClientConfiguration.ConfigureHttpRequestMessageAsync(
            this,
            httpRequestMessage,
            cancellationToken
        );

        return httpRequestMessage;
    }
}
