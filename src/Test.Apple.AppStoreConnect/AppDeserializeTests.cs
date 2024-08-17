using Apple.AppStoreConnect;
using Apple.AppStoreConnect.Converters;
using Apple.AppStoreConnect.Interfaces;
using H;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Text.Json;
using Xunit;

namespace Test.Apple.AppStoreConnect;

public class AppDeserializeTests
{
    [Fact]
    public void DeserializeAppsResponseWorks()
    {
        var appsResponse = JsonSerializer.Deserialize<AppsResponse>(
            Resources.app1_json.AsStream(),
            CreateSerializerSettings()
        );

        Assert.NotNull(appsResponse);
    }

    private static JsonSerializerOptions CreateSerializerSettings()
    {
        var serviceCollection = new ServiceCollection()
            .AddLogging()
            .AddMemoryCache()
            .AddSingleton<TimeProvider, FakeTimeProvider>()
            .AddSingleton<IJwtGenerator, DefaultJwtGenerator>()
            .AddSingleton<JsonStringEnumConverterFactory>()
            .AddSingleton<OneOfJsonConverterFactory>()
            .AddSingleton<IHttpClientConfiguration, DefaultHttpClientConfiguration>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var jsonSerializerOptions = new JsonSerializerOptions();

        var httpClientConfiguration = serviceProvider.GetRequiredService<IHttpClientConfiguration>();
        foreach (var jsonConverter in httpClientConfiguration.GetJsonConverters())
        {
            jsonSerializerOptions.Converters.Add(jsonConverter);
        }

        return jsonSerializerOptions;
    }
}
