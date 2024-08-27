using Apple.AppStoreConnect.Client.Models;
using H;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using System.Threading.Tasks;
using Xunit;

namespace Apple.AppStoreConnect.Tests;

public class AppDeserializeTests
{
    [Fact]
    public async Task DeserializeAppsResponseWorks()
    {
        var jsonParseNodeFactory = new JsonParseNodeFactory();
        ApiClientBuilder.RegisterDefaultDeserializer<JsonParseNodeFactory>();

        var appsResponse = await KiotaSerializer.DeserializeAsync<AppsResponse>(
            jsonParseNodeFactory.ValidContentType,
            Resources.app1_json.AsStream()
        );

        Assert.NotNull(appsResponse);
        Assert.Equal(2, appsResponse.Data!.Count);
    }
}
