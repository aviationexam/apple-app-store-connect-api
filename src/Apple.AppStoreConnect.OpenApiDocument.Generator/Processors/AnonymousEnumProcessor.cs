using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator.Processors;

public static class AnonymousEnumProcessor
{
    public static bool TryProcessItem(
        IReadOnlyCollection<PathItem> path,
        ReadOnlySpan<byte> lastProperty,
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter,
        TransposeContext context
    )
    {
        if (
            lastProperty.SequenceEqual("schema"u8)
            && path.Count == 6
            && path.Take(2).SequenceEqual(new PathItem[]
            {
                new(JsonTokenType.StartObject, null),
                new(JsonTokenType.StartArray, "parameters"),
            })
            && path.ElementAt(2).TokenType == JsonTokenType.StartObject
            && path.ElementAt(3).TokenType == JsonTokenType.StartObject
            && path.Skip(4).SequenceEqual(new PathItem[]
            {
                new(JsonTokenType.StartObject, "paths"),
                new(JsonTokenType.StartObject, null),
            })
        )
        {
            var jsonReaderClone = jsonReader;

            if (TryReadInner(ref jsonReaderClone, jsonWriter, context))
            {
                jsonWriter.WriteNullValue();

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
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter,
        TransposeContext context
    )
    {
        var innerJson = new StringBuilder();

        var path = new Stack<PathItem>();
        ReadOnlySpan<byte> lastProperty = default;

        var arrayTypeCheck = false;
        var arrayItemTypeCheck = false;

        var enumValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (jsonReader.Read())
        {
            var tokenType = jsonReader.TokenType;

            switch (tokenType)
            {
                case JsonTokenType.StartObject:
                    innerDepth++;
                    path.WritePathItem(tokenType, ref lastProperty);

                    break;
                case JsonTokenType.EndObject:
                    innerDepth--;
                    if (path.Count > 0)
                    {
                        path.Pop();
                    }

                    break;
                case JsonTokenType.StartArray:
                    innerDepth++;
                    path.WritePathItem(tokenType, ref lastProperty);

                    break;
                case JsonTokenType.EndArray:
                    innerDepth--;
                    if (path.Count > 0)
                    {
                        path.Pop();
                    }

                    break;
                case JsonTokenType.PropertyName:
                    if (jsonReader.HasValueSequence)
                    {
                        throw new Exception();
                    }

                    lastProperty = jsonReader.ValueSpan;
                    var propertyName = Encoding.UTF8.GetString(jsonReader.ValueSpan.ToArray());

                    innerJson.Append($"\"{propertyName}\":");

                    break;

                case JsonTokenType.None:
                    break;
                case JsonTokenType.Comment:
                    break;

                case JsonTokenType.String:
                    if (jsonReader.HasValueSequence)
                    {
                        throw new Exception();
                    }

                    if (
                        innerDepth == 1
                        && !lastProperty.IsEmpty
                        && lastProperty.SequenceEqual("type"u8)
                        && !jsonReader.ValueSpan.SequenceEqual("array"u8)
                    )
                    {
                        return false;
                    }

                    var value = Encoding.UTF8.GetString(jsonReader.ValueSpan.ToArray());
                    innerJson.Append('"' + value + '"');

                    break;
                case JsonTokenType.Number:
                case JsonTokenType.True:
                case JsonTokenType.False:
                case JsonTokenType.Null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (innerDepth == 0)
            {
                break;
            }
        }

        if (!arrayTypeCheck || !arrayItemTypeCheck)
        {
            return false;
        }

        var reference = context.GetEnumComponentReference(enumValues);

        /*
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
                    var reference = context.GetEnumComponentReference(enums);

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
            */

        return true;
    }
}
