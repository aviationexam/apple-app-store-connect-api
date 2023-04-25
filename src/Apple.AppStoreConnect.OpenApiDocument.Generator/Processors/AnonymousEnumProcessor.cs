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

            if (
                path.ElementAt(0).Properties.TryGetValue("name", out var name)
                && path.ElementAt(2).Properties.TryGetValue("tags", out var tag)
                && path.ElementAt(2).Properties.TryGetValue("operationId", out var operationId)
                && TryReadInner(path, tag, name, operationId, ref jsonReaderClone, jsonWriter, context)
            )
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
            foreach (var enumComponentValue in context.EnumComponentValues)
            {
                jsonWriter.WritePropertyName(enumComponentValue.Key);
                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("type"u8);
                jsonWriter.WriteStringValue("string"u8);

                jsonWriter.WritePropertyName("enum"u8);
                jsonWriter.WriteStartArray();

                foreach (var enumValue in enumComponentValue.Value)
                {
                    jsonWriter.WriteStringValue(enumValue);
                }

                jsonWriter.WriteEndArray();

                jsonWriter.WriteEndObject();
            }

            return true;
        }

        return false;
    }

    private static bool TryReadInner(
        IReadOnlyCollection<PathItem> parentPath,
        string tag, string name, string operationId,
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
                    path.WritePathItem(tokenType, ref lastProperty);

                    break;
                case JsonTokenType.EndObject:
                    if (path.Count > 0)
                    {
                        path.Pop();
                    }

                    break;
                case JsonTokenType.StartArray:
                    path.WritePathItem(tokenType, ref lastProperty);

                    break;
                case JsonTokenType.EndArray:
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
                        !lastProperty.IsEmpty
                        && lastProperty.SequenceEqual("type"u8)
                    )
                    {
                        if (path.Count == 1)
                        {
                            if (jsonReader.ValueSpan.SequenceEqual("array"u8))
                            {
                                arrayTypeCheck = true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else if (
                            path.Count == 2
                            && path.SequenceEqual(new PathItem[]
                            {
                                new(JsonTokenType.StartObject, "items"),
                                new(JsonTokenType.StartObject, null),
                            })
                        )
                        {
                            if (jsonReader.ValueSpan.SequenceEqual("string"u8))
                            {
                                arrayItemTypeCheck = true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else if (
                        lastProperty.IsEmpty
                        && path.SequenceEqual(new PathItem[]
                        {
                            new(JsonTokenType.StartArray, "enum"),
                            new(JsonTokenType.StartObject, "items"),
                            new(JsonTokenType.StartObject, null),
                        })
                    )
                    {
                        enumValues.Add(Encoding.UTF8.GetString(jsonReader.ValueSpan.ToArray()));
                    }

                    break;
                case JsonTokenType.Number:
                case JsonTokenType.True:
                case JsonTokenType.False:
                case JsonTokenType.Null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (path.Count == 0)
            {
                break;
            }
        }

        if (!arrayTypeCheck || !arrayItemTypeCheck)
        {
            return false;
        }

        var reference = context.GetEnumComponentReference(
            parentPath,
            tag, name.AsSpan(),
            operationId.AsSpan(),
            enumValues
        );

        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("type"u8);
        jsonWriter.WriteStringValue("array"u8);

        jsonWriter.WritePropertyName("items"u8);
        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("$ref"u8);
        jsonWriter.WriteStringValue(reference);
        jsonWriter.WriteEndObject();

        jsonWriter.WriteEndObject();

        return true;
    }
}
