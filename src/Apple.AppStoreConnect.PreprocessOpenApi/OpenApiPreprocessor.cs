using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.PreprocessOpenApi;

public sealed class OpenApiPreprocessor(
    string source,
    string target,
    string versionFile
)
{
    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    private const string Info = "info";
    private static ReadOnlySpan<byte> Version => "version"u8;

    // Apple's spec puts a discriminator on AppAssetLibraryPlacement.properties.relationships whose propertyName
    // (mediaType) is defined on the sibling "attributes" object, not on either oneOf branch, so it can never match.
    // Microsoft.OpenApi rejects it (ValidateSchemaDiscriminator), and the generated factory never selects a branch,
    // which silently drops all relationship data. Neither branch forbids additional properties, so every payload
    // matches both of them; anyOf without a discriminator describes that correctly. Kiota then generates an
    // intersection wrapper that populates both branches. Applied only while the spec still has this exact shape.
    private static readonly string?[] AppAssetLibraryPlacementRelationshipsPath =
        [null, "components", "schemas", "AppAssetLibraryPlacement", "properties", "relationships"];

    private static readonly string?[] AppAssetLibraryPlacementRelationshipsDiscriminatorPath =
        [.. AppAssetLibraryPlacementRelationshipsPath, "discriminator"];

    private static ReadOnlySpan<byte> Discriminator => "discriminator"u8;
    private static ReadOnlySpan<byte> PropertyName => "propertyName"u8;
    private static ReadOnlySpan<byte> MediaType => "mediaType"u8;
    private static ReadOnlySpan<byte> OneOf => "oneOf"u8;
    private static ReadOnlySpan<byte> AnyOf => "anyOf"u8;

    public void Preprocess()
    {
        ReadOnlySpan<byte> jsonReadOnlySpan = File.ReadAllBytes(source);

        // Read past the UTF-8 BOM bytes if a BOM exists.
        if (jsonReadOnlySpan.StartsWith(Utf8Bom))
        {
            jsonReadOnlySpan = jsonReadOnlySpan[Utf8Bom.Length..];
        }

        var readerOptions = new JsonReaderOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
        };

        var reader = new Utf8JsonReader(jsonReadOnlySpan, readerOptions);

        var writerOptions = new JsonWriterOptions
        {
            Indented = true,
        };

        using var targetStream = File.OpenWrite(target);
        using var writer = new Utf8JsonWriter(targetStream, writerOptions);

        var collectedMetadata = Collect(reader);

        if (collectedMetadata.GetVersion() is { } version)
        {
            var apiMajor = version.Major;
            var apiMinor = version.Minor;
            var apiBuild = version.Build;

            if (apiBuild == -1)
            {
                apiBuild = 0;
            }

            File.WriteAllText(versionFile, $"{version}\n{apiMajor:00}{apiMinor:00}{apiBuild:00}");
        }

        Preprocess(ref reader, writer, collectedMetadata);
    }

    private static bool PathEquals(Stack<TreeItem> path, string?[] expected)
    {
        if (path.Count != expected.Length)
        {
            return false;
        }

        // Stack enumerates from the innermost item outwards.
        var index = expected.Length - 1;
        foreach (var item in path)
        {
            if (item.PropertyName != expected[index--])
            {
                return false;
            }
        }

        return true;
    }

    private CollectedMetadata Collect(Utf8JsonReader reader)
    {
        var collectedMetadata = new CollectedMetadata();
        var currentPath = new Stack<TreeItem>();
        ReadOnlySpan<byte> lastProperty = default;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    lastProperty = reader.ValueSpan;

                    break;

                case JsonTokenType.StartArray:
                case JsonTokenType.StartObject:
                    currentPath.Push(new TreeItem(reader.TokenType, lastProperty.IsEmpty ? null : Encoding.UTF8.GetString(lastProperty)));
                    lastProperty = default;

                    break;
                case JsonTokenType.EndArray:
                case JsonTokenType.EndObject:
                    currentPath.Pop();

                    lastProperty = default;

                    break;
                case JsonTokenType.String:
                    if (
                        lastProperty.SequenceEqual(Version)
                        && currentPath.Count == 2
                        && currentPath.ToArray() is
                        [
                        { JsonTokenType: JsonTokenType.StartObject, PropertyName: Info },
                        { JsonTokenType: JsonTokenType.StartObject, PropertyName: null },
                        ]
                    )
                    {
                        collectedMetadata.AddVersion(reader.ValueSpan);
                    }
                    else if (
                        lastProperty.SequenceEqual(PropertyName)
                        && reader.ValueTextEquals(MediaType)
                        && PathEquals(currentPath, AppAssetLibraryPlacementRelationshipsDiscriminatorPath)
                    )
                    {
                        collectedMetadata.RequireAppAssetLibraryPlacementRelationshipsFix();
                    }

                    break;
                case JsonTokenType.Number:

                    break;
                case JsonTokenType.True:

                    break;
                case JsonTokenType.False:

                    break;
                case JsonTokenType.Null:

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(reader.TokenType), reader.TokenType, null);
            }
        }

        return collectedMetadata;
    }

    private void Preprocess(ref Utf8JsonReader reader, Utf8JsonWriter writer, CollectedMetadata collectedMetadata)
    {
        var currentPath = new Stack<TreeItem>();
        ReadOnlySpan<byte> lastProperty = default;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    lastProperty = reader.ValueSpan;

                    if (
                        collectedMetadata.FixAppAssetLibraryPlacementRelationships
                        && PathEquals(currentPath, AppAssetLibraryPlacementRelationshipsPath)
                    )
                    {
                        if (reader.ValueTextEquals(Discriminator))
                        {
                            reader.Read();
                            reader.Skip();
                            lastProperty = default;

                            break;
                        }

                        if (reader.ValueTextEquals(OneOf))
                        {
                            writer.WritePropertyName(AnyOf);

                            break;
                        }
                    }

                    writer.WritePropertyName(reader.ValueSpan);

                    break;

                case JsonTokenType.StartArray:
                case JsonTokenType.StartObject:
                    currentPath.Push(new TreeItem(reader.TokenType, lastProperty.IsEmpty ? null : Encoding.UTF8.GetString(lastProperty)));
                    lastProperty = default;

                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        writer.WriteStartArray();
                    }
                    else
                    {
                        writer.WriteStartObject();
                    }

                    break;
                case JsonTokenType.EndArray:
                    currentPath.Pop();
                    lastProperty = default;
                    writer.WriteEndArray();

                    break;
                case JsonTokenType.EndObject:
                    currentPath.Pop();
                    lastProperty = default;
                    writer.WriteEndObject();

                    break;
                case JsonTokenType.String:
                    writer.WriteStringValue(reader.ValueSpan);

                    break;
                case JsonTokenType.Number:

                    writer.WriteRawValue(reader.ValueSpan);

                    break;
                case JsonTokenType.True:
                    writer.WriteBooleanValue(true);

                    break;
                case JsonTokenType.False:
                    writer.WriteBooleanValue(false);

                    break;
                case JsonTokenType.Null:
                    writer.WriteNullValue();

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(reader.TokenType), reader.TokenType, null);
            }
        }
    }
}
