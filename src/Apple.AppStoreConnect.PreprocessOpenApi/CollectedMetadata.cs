using System.Text;

namespace Apple.AppStoreConnect.PreprocessOpenApi;

public sealed class CollectedMetadata
{
    private Version? _targetApiVersion;

    public void AddVersion(ReadOnlySpan<byte> target)
    {
        _targetApiVersion = Version.Parse(Encoding.UTF8.GetString(target));
    }

    public Version? GetVersion() => _targetApiVersion;

    public bool FixAppAssetLibraryPlacementRelationships { get; private set; }

    public void RequireAppAssetLibraryPlacementRelationshipsFix() => FixAppAssetLibraryPlacementRelationships = true;
}
