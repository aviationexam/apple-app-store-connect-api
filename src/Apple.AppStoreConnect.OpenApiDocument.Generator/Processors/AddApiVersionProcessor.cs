using Apple.AppStoreConnect.GeneratorCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator.Processors;

public static class AddApiVersionProcessor
{
    public static bool TryProcessItem(
        IReadOnlyCollection<PathItem> path,
        ReadOnlySpan<byte> lastProperty,
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter
    )
    {
        if (
            lastProperty.SequenceEqual("tags"u8)
            && path.Count == 4
            && path.ElementAt(0).TokenType is JsonTokenType.StartObject
            && path.ElementAt(1) is { TokenType: JsonTokenType.StartObject, PropertyName: not null } routeElement
            && path.Skip(2).SequenceEqual(new PathItem[]
            {
                new(JsonTokenType.StartObject, "paths"),
                new(JsonTokenType.StartObject, null),
            })
        )
        {
            var routeSpan = routeElement.PropertyName.AsSpan();

            var slashIndex = routeSpan.IndexOf('/') + 1;
            routeSpan = routeSpan[slashIndex..];

            slashIndex = routeSpan.IndexOf('/');
            var version = routeSpan[..slashIndex].ToArray().AsSpan();
            if (char.IsLower(version[0]))
            {
                version[0] = char.ToUpperInvariant(version[0]);
            }

            var versionBytes = Encoding.UTF8.GetBytes(version.ToArray()).AsSpan();

            while (jsonReader.Read())
            {
                var tokenType = jsonReader.TokenType;

                if (tokenType == JsonTokenType.StartArray)
                {
                    jsonWriter.WriteStartArray();
                }
                else if (tokenType == JsonTokenType.String)
                {
                    var stringValueSpan = jsonReader.ValueSpan;
                    var tagWithVersion = new byte[stringValueSpan.Length + versionBytes.Length].AsSpan();
                    stringValueSpan.CopyTo(tagWithVersion);
                    versionBytes.CopyTo(tagWithVersion.Slice(stringValueSpan.Length, versionBytes.Length));

                    jsonWriter.WriteStringValue(tagWithVersion);
                }
                else if (tokenType == JsonTokenType.EndArray)
                {
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
