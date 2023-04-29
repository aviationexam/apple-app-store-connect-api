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
            lastProperty.SequenceEqual("attributes"u8)
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
            var jsonDocument = JsonDocument.ParseValue(ref jsonReader);

            var property = jsonDocument.RootElement.ToString();

            var component = path.ElementAt(1)!;

            var componentNameLength = component.PropertyName!.Length;

            var titleSpan = new char[
                componentNameLength
                + lastProperty.Length
            ].AsSpan();

            component.PropertyName.AsSpan().CopyTo(titleSpan);
            Encoding.UTF8.GetString(lastProperty.ToArray())
                .AsSpan()
                .CopyTo(titleSpan[componentNameLength..]);

            if (char.IsLower(titleSpan[componentNameLength]))
            {
                titleSpan[componentNameLength] = char.ToUpperInvariant(titleSpan[componentNameLength]);
            }

            var referenceName = context.AddComponent(
                titleSpan.ToString(),
                property
            );

            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("$ref"u8);
            jsonWriter.WriteStringValue(referenceName);
            jsonWriter.WriteEndObject();
        }

        return false;
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
