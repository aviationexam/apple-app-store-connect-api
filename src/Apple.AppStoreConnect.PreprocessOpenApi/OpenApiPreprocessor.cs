using Apple.AppStoreConnect.PreprocessOpenApi;
using System.Text;
using System.Text.Json;

public sealed class OpenApiPreprocessor(
    string source,
    string target
)
{
    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];
    private static ReadOnlySpan<byte> OneOf => "oneOf"u8;
    private static ReadOnlySpan<byte> Ref => "$ref"u8;

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
                    currentPath.Push(new TreeItem(reader.TokenType, lastProperty.IsEmpty ? null : Encoding.UTF8.GetString(lastProperty)));

                    break;
                case JsonTokenType.StartObject:
                    currentPath.Push(new TreeItem(reader.TokenType, lastProperty.IsEmpty ? null : Encoding.UTF8.GetString(lastProperty)));

                    break;
                case JsonTokenType.EndArray:
                    currentPath.Pop();

                    break;
                case JsonTokenType.EndObject:
                    currentPath.Pop();

                    break;
                case JsonTokenType.String:
                    if (
                        lastProperty.SequenceEqual(Ref)
                    )
                    {
                        collectedMetadata.AddOneOf(currentPath, reader.ValueSpan);
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

    private void Preprocess(ref Utf8JsonReader reader, CollectedMetadata collectedMetadata, Utf8JsonWriter writer)
    {
        var currentPath = new Stack<string>();
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
                    currentPath.Push(Encoding.UTF8.GetString(lastProperty));

                    writer.WriteStartArray();

                    break;
                case JsonTokenType.StartObject:
                    currentPath.Push(Encoding.UTF8.GetString(lastProperty));

                    writer.WriteStartObject();

                    break;
                case JsonTokenType.EndArray:
                    currentPath.Pop();

                    writer.WriteEndArray();

                    break;
                case JsonTokenType.EndObject:
                    currentPath.Pop();

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
