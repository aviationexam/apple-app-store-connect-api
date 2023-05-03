using Apple.AppStoreConnect.GeneratorCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator.Processors;

public static class SubscriptionStatusUrlVersionProcessor
{
    public static bool TryProcessItem(
        IReadOnlyCollection<PathItem> path,
        ReadOnlySpan<byte> lastProperty,
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter
    )
    {
        if (
            lastProperty.SequenceEqual("enum"u8)
            && path.SequenceEqual(new PathItem[]
            {
                new(JsonTokenType.StartObject, "SubscriptionStatusUrlVersion"),
                new(JsonTokenType.StartObject, "schemas"),
                new(JsonTokenType.StartObject, "components"),
                new(JsonTokenType.StartObject, null),
            })
        )
        {
            var enums = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (jsonReader.Read())
            {
                var tokenType = jsonReader.TokenType;

                if (tokenType == JsonTokenType.StartArray)
                {
                    jsonWriter.WriteStartArray();
                }
                else if (tokenType == JsonTokenType.String)
                {
                    enums.Add(jsonReader.GetString()!);
                }
                else if (tokenType == JsonTokenType.EndArray)
                {
                    foreach (var enumValue in enums)
                    {
                        jsonWriter.WriteStringValue(enumValue);
                    }

                    jsonWriter.WriteEndArray();
                    break;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, "");
                }
            }

            return true;
        }

        return false;
    }
}
