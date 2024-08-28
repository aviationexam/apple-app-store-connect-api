using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.PreprocessOpenApi;

public sealed class OpenApiPreprocessor(
    string source,
    string target
)
{
    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    private const string OneOf = "oneOf";
    private static ReadOnlySpan<byte> Ref => "$ref"u8;
    private const string Properties = "properties";
    private const string Type = "type";
    private const string Enum = "enum";
    private const string Schemas = "schemas";
    private const string Components = "components";
    private static ReadOnlySpan<byte> Deprecated => "deprecated"u8;

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

        Preprocess(ref reader, collectedMetadata, writer);
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
                        lastProperty.SequenceEqual(Ref)
                        && currentPath.ToArray() is
                        [
                            { JsonTokenType: JsonTokenType.StartObject, PropertyName: null },
                            { JsonTokenType: JsonTokenType.StartArray, PropertyName: OneOf },
                            ..
                        ]
                    )
                    {
                        collectedMetadata.AddOneOf(currentPath, reader.ValueSpan);
                    }
                    else if (
                        lastProperty.IsEmpty
                        && currentPath.Count == 7
                        && currentPath.ToArray() is
                        [
                            { JsonTokenType: JsonTokenType.StartArray, PropertyName: Enum },
                            { JsonTokenType: JsonTokenType.StartObject, PropertyName: Type },
                            { JsonTokenType: JsonTokenType.StartObject, PropertyName: Properties },
                            ..
                        ]
                    )
                    {
                        collectedMetadata.AddComponentType(currentPath, reader.ValueSpan);
                    }

                    break;
                case JsonTokenType.Number:

                    break;
                case JsonTokenType.True:
                    if (
                        lastProperty.SequenceEqual(Deprecated)
                        && currentPath.Count == 4
                        && currentPath.ToArray() is
                        [
                            not null,
                            { JsonTokenType: JsonTokenType.StartObject, PropertyName: Schemas },
                            { JsonTokenType: JsonTokenType.StartObject, PropertyName: Components },
                            { JsonTokenType: JsonTokenType.StartObject, PropertyName: null }
                        ]
                    )
                    {
                        collectedMetadata.AddDeprecatedType(currentPath);
                    }

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

    private void Preprocess(ref Utf8JsonReader reader, CollectedMetadata collectedMetadata, Utf8JsonWriter writer)
    {
        var currentPath = new Stack<TreeItem>();
        ReadOnlySpan<byte> lastProperty = default;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    lastProperty = reader.ValueSpan;

                    writer.WritePropertyName(reader.ValueSpan);

                    break;

                case JsonTokenType.StartArray:
                    currentPath.Push(new TreeItem(reader.TokenType, lastProperty.IsEmpty ? null : Encoding.UTF8.GetString(lastProperty)));
                    lastProperty = default;

                    writer.WriteStartArray();

                    break;
                case JsonTokenType.StartObject:
                    currentPath.Push(new TreeItem(reader.TokenType, lastProperty.IsEmpty ? null : Encoding.UTF8.GetString(lastProperty)));
                    lastProperty = default;

                    writer.WriteStartObject();

                    break;
                case JsonTokenType.EndArray:
                    var leavingProperty = currentPath.Pop();
                    lastProperty = default;

                    writer.WriteEndArray();

                    if (
                        leavingProperty is { PropertyName: OneOf, JsonTokenType: JsonTokenType.StartArray }
                        && collectedMetadata.HasKnownOneOfMapping(currentPath)
                    )
                    {
                        writer.WritePropertyName("discriminator"u8);

                        writer.WriteStartObject();
                        {
                            writer.WritePropertyName("propertyName"u8);
                            writer.WriteStringValue(Type);

                            writer.WritePropertyName("mapping"u8);
                            writer.WriteStartObject();
                            {
                                foreach (var (discriminator, reference) in collectedMetadata.GetOneOfMapping(currentPath))
                                {
                                    writer.WritePropertyName(discriminator);
                                    writer.WriteStringValue(reference);
                                }
                            }
                            writer.WriteEndObject();
                        }
                        writer.WriteEndObject();
                    }

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
