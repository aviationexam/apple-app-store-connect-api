using Apple.AppStoreConnect.Converters;
using Apple.AppStoreConnect.Interfaces;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Apple.AppStoreConnect;

public sealed class DefaultHttpClientConfiguration : IHttpClientConfiguration
{
    private readonly IJwtGenerator _jwtGenerator;
    private readonly JsonStringEnumConverterFactory _jsonStringEnumConverterFactory;
    private readonly OneOfJsonConverterFactory _oneOfJsonConverterFactory;

    public DefaultHttpClientConfiguration(
        IJwtGenerator jwtGenerator,
        JsonStringEnumConverterFactory jsonStringEnumConverterFactory,
        OneOfJsonConverterFactory oneOfJsonConverterFactory
    )
    {
        _jwtGenerator = jwtGenerator;
        _jsonStringEnumConverterFactory = jsonStringEnumConverterFactory;
        _oneOfJsonConverterFactory = oneOfJsonConverterFactory;
    }

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
                await _jwtGenerator.GenerateJwtTokenAsync(cancellationToken)
            );
        }
    }

    public IEnumerable<JsonConverter> GetJsonConverters()
    {
        yield return _jsonStringEnumConverterFactory;
        yield return _oneOfJsonConverterFactory;
    }
}
