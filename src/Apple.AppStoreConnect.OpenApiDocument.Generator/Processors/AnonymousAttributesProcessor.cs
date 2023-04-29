using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator.Processors;

public static class AnonymousAttributesProcessor
{
    public static bool TryProcessItem(
        IReadOnlyCollection<PathItem> path,
        ReadOnlySpan<byte> lastProperty,
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter,
        TransposeContext context
    )
    {
        if (
            (
                lastProperty.SequenceEqual("data"u8)
                || lastProperty.SequenceEqual("attributes"u8)
                || lastProperty.SequenceEqual("relationships"u8)
            )
            && path.Count == 5
            && path.Take(1).SequenceEqual(new PathItem[]
            {
                new(JsonTokenType.StartObject, "properties"),
            })
            && path.ElementAt(1).TokenType == JsonTokenType.StartObject
            && path.Skip(2).SequenceEqual(new PathItem[]
            {
                new(JsonTokenType.StartObject, "schemas"),
                new(JsonTokenType.StartObject, "components"),
                new(JsonTokenType.StartObject, null),
            })
        )
        {
            var jsonReaderClone = jsonReader;

            if (
                !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.StartObject
                || !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.PropertyName
                || !jsonReaderClone.ValueSpan.SequenceEqual("type"u8)
                || !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.String
                || !jsonReaderClone.ValueSpan.SequenceEqual("object"u8)
            )
            {
                // verify there is object and not a reference
                return false;
            }

            var component = path.ElementAt(1)!;
            var lastPropertySpan = Encoding.UTF8.GetString(lastProperty.ToArray()).AsSpan();

            using var jsonDocument = JsonDocument.ParseValue(ref jsonReader);
            var jsonDocumentContent = jsonDocument.RootElement;

            var referenceName = ProcessItemInternal(
                component.PropertyName!.AsSpan(),
                lastPropertySpan,
                ref jsonDocumentContent,
                jsonWriter,
                context
            );

            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("$ref"u8);
            jsonWriter.WriteStringValue(referenceName);
            jsonWriter.WriteEndObject();
        }

        return false;
    }

    private static string ProcessItemInternal(
        ReadOnlySpan<char> typePrefix,
        ReadOnlySpan<char> lastPropertySpan,
        ref JsonElement jsonDocumentContent,
        Utf8JsonWriter jsonWriter,
        TransposeContext context
    )
    {
        var componentNameLength = typePrefix.Length;

        var titleSpan = new char[
            componentNameLength
            + lastPropertySpan.Length
        ].AsSpan();

        typePrefix.CopyTo(titleSpan);
        lastPropertySpan.CopyTo(titleSpan[componentNameLength..]);

        if (char.IsLower(titleSpan[componentNameLength]))
        {
            titleSpan[componentNameLength] = char.ToUpperInvariant(titleSpan[componentNameLength]);
        }

        if (jsonDocumentContent.TryGetProperty("properties"u8, out var innerProperties))
        {
            foreach (var innerProperty in innerProperties.EnumerateObject())
            {
                if (
                    innerProperty.Name
                        is "attributes"
                        or "relationships"
                    && innerProperty.Value.TryGetProperty("properties"u8, out var subProperty))
                {
                    var referenceName = ProcessItemInternal(
                        titleSpan,
                        innerProperty.Name.AsSpan(),
                        ref subProperty,
                        jsonWriter,
                        context
                    );
                }
            }
        }

        var property = jsonDocumentContent.ToString();

        return context.AddComponent(
            titleSpan.ToString(),
            property
        );
    }

    public static bool TryWriteAdditional(
        PathItem? pathItem,
        IReadOnlyCollection<PathItem> path,
        Utf8JsonWriter jsonWriter,
        TransposeContext context
    )
    {
        if (
            pathItem == new PathItem(JsonTokenType.StartObject, "schemas")
            && path.SequenceEqual(new PathItem[]
            {
                new(JsonTokenType.StartObject, "components"),
                new(JsonTokenType.StartObject, null),
            })
        )
        {
            foreach (var renamedComponentValue in context.RenamedComponentValues)
            {
                jsonWriter.WritePropertyName(renamedComponentValue.Key);
                jsonWriter.WriteRawValue(renamedComponentValue.Value);
            }

            return true;
        }

        return false;
    }
}
