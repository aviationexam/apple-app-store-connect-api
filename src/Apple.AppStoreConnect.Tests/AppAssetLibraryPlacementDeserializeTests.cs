using Apple.AppStoreConnect.Client.Models;
using H;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Apple.AppStoreConnect.Tests;

// Apple's spec discriminates AppAssetLibraryPlacement.relationships on a property that only exists on the sibling
// attributes object. PreprocessOpenApi rewrites it to an anyOf, so both branches are populated from the same node.
// Kiota merges their deserializers first-wins, so the shared relationships land on the image branch.
public class AppAssetLibraryPlacementDeserializeTests
{
    private static async Task<AppAssetLibraryPlacementResponse?> DeserializeAsync(Stream stream)
    {
        var jsonParseNodeFactory = new JsonParseNodeFactory();
        ApiClientBuilder.RegisterDefaultDeserializer<JsonParseNodeFactory>();

        return await KiotaSerializer.DeserializeAsync<AppAssetLibraryPlacementResponse>(
            jsonParseNodeFactory.ValidContentType,
            stream,
            TestContext.Current.CancellationToken
        );
    }

    [Fact]
    public async Task DeserializeVideoPlacementRelationshipsWorks()
    {
        var response = await DeserializeAsync(Resources.app_asset_library_placement_video_json.AsStream());

        var relationships = response?.Data?.Relationships;
        Assert.NotNull(relationships);

        Assert.Equal("video-1", relationships.AppAssetLibraryPlacementVideoRelationships?.Video?.Data?.Id);
        Assert.Equal("localization-1", relationships.AppAssetLibraryPlacementImageRelationships?.AppStoreVersionLocalization?.Data?.Id);
        Assert.Null(relationships.AppAssetLibraryPlacementImageRelationships?.Image);
        Assert.Equal(AppAssetLibraryMediaType.VIDEO, response!.Data!.Attributes?.MediaType);
    }

    [Fact]
    public async Task DeserializeImagePlacementRelationshipsWorks()
    {
        var response = await DeserializeAsync(Resources.app_asset_library_placement_image_json.AsStream());

        var relationships = response?.Data?.Relationships;
        Assert.NotNull(relationships);

        Assert.Equal("image-1", relationships.AppAssetLibraryPlacementImageRelationships?.Image?.Data?.Id);
        Assert.Equal("event-localization-1", relationships.AppAssetLibraryPlacementImageRelationships?.AppEventLocalization?.Data?.Id);
        Assert.Null(relationships.AppAssetLibraryPlacementVideoRelationships?.Video);
        Assert.Equal(AppAssetLibraryMediaType.IMAGE, response!.Data!.Attributes?.MediaType);
    }
}
