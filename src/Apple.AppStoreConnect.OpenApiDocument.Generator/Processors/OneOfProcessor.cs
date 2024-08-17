using Apple.AppStoreConnect.GeneratorCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator.Processors;

public static class OneOfProcessor
{
    public static bool TryProcessItem(
        IReadOnlyCollection<PathItem> path,
        ReadOnlySpan<byte> lastProperty,
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter,
        TransposeContext context
    )
    {
        if (lastProperty.SequenceEqual("items"u8))
        {
            var jsonReaderClone = jsonReader;

            if (
                !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.StartObject
                || !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.PropertyName
                || !jsonReaderClone.ValueSpan.SequenceEqual("oneOf"u8)
                || !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.StartArray
            )
            {
                return false;
            }

            if (TryReadInner(path, ref jsonReaderClone, jsonWriter, context))
            {
                var innerTokenStartIndex = jsonReaderClone.TokenStartIndex;

                while (innerTokenStartIndex > jsonReader.TokenStartIndex && jsonReader.Read())
                {
                    jsonReader.Skip();
                }

                return true;
            }
        }

        return false;
    }

    private static bool TryReadInner(
        IReadOnlyCollection<PathItem> parentPath,
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter,
        TransposeContext context
    )
    {
        var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ReadOnlySpan<byte> lastProperty = default;
        while (jsonReader.Read())
        {
            switch (jsonReader.TokenType)
            {
                case JsonTokenType.EndArray:
                    var oneOfReference = context.GetOneOfComponentReference(
                        parentPath.ElementAt(2).PropertyName!.AsSpan(),
                        parentPath.ElementAt(0).PropertyName!.AsSpan(),
                        references
                    );

                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("$ref"u8);
                    jsonWriter.WriteStringValue(oneOfReference);
                    jsonWriter.WriteEndObject();

                    return true;

                case JsonTokenType.StartObject:
                    break;
                case JsonTokenType.EndObject:
                    lastProperty = null;
                    break;

                case JsonTokenType.PropertyName:
                    if (jsonReader.HasValueSequence)
                    {
                        throw new Exception();
                    }

                    lastProperty = jsonReader.ValueSpan;

                    break;

                case JsonTokenType.String:
                    if (lastProperty.SequenceEqual("$ref"u8))
                    {
                        if (
                            jsonReader.ValueTextEquals("#/components/schemas/InAppPurchase"u8)
                            && parentPath.ElementAt(2).PropertyName == "AppsResponse"
                        )
                        {
                            lastProperty = null;
                            break;
                        }

                        references.Add(Encoding.UTF8.GetString(jsonReader.ValueSpan.ToArray()));
                        lastProperty = null;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(jsonReader.TokenType), jsonReader.TokenType,
                        $"Unexpected {nameof(jsonReader.TokenType)}"
                    );
            }
        }

        return false;
    }
}
