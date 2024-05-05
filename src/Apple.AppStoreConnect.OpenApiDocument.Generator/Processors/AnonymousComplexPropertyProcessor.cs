using Apple.AppStoreConnect.GeneratorCommon;
using Apple.AppStoreConnect.OpenApiDocument.Generator.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator.Processors;

public static class AnonymousComplexPropertyProcessor
{
    public static bool TryProcessItem(
        IReadOnlyCollection<PathItem> path,
        ReadOnlySpan<byte> lastProperty,
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter,
        TransposeContext context
    )
    {
        if (
            path.Count == 5
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
            )
            {
                return false;
            }

            var isObject = jsonReaderClone.ValueSpan.SequenceEqual("object"u8);
            var isArray = jsonReaderClone.ValueSpan.SequenceEqual("array"u8);
            var isString = jsonReaderClone.ValueSpan.SequenceEqual("string"u8);

            if (!isObject && !isArray && !isString)
            {
                // verify there is object and not a reference
                return false;
            }

            if (
                isArray && (
                    !jsonReaderClone.Read()
                    || jsonReaderClone.TokenType is not JsonTokenType.PropertyName
                    || !jsonReaderClone.ValueSpan.SequenceEqual("items"u8)
                    || !jsonReaderClone.Read()
                    || jsonReaderClone.TokenType is not JsonTokenType.StartObject
                    || !jsonReaderClone.Read()
                    || jsonReaderClone.TokenType is not JsonTokenType.PropertyName
                    || !jsonReaderClone.ValueSpan.SequenceEqual("type"u8)
                    || !jsonReaderClone.Read()
                    || jsonReaderClone.TokenType is not JsonTokenType.String
                    || (
                        !jsonReaderClone.ValueSpan.SequenceEqual("array"u8)
                        && !jsonReaderClone.ValueSpan.SequenceEqual("object"u8)
                    )
                )
            )
            {
                // verify there is array of objects and not a references
                return false;
            }

            var component = path.ElementAt(1)!;
            var lastPropertySpan = Encoding.UTF8.GetString(lastProperty.ToArray()).AsSpan();

            var jsonNode = JsonNode.Parse(ref jsonReader) ?? throw new NullReferenceException("jsonNode");

            if (isObject)
            {
                var referenceName = ProcessItemInternal(
                    component.PropertyName!.AsSpan(),
                    lastPropertySpan,
                    jsonNode,
                    context
                );

                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("$ref"u8);
                jsonWriter.WriteStringValue(referenceName);
                jsonWriter.WriteEndObject();
            }
            else if (isArray)
            {
                jsonNode["items"] = ProcessItemInternal(
                    component.PropertyName!.AsSpan(),
                    lastPropertySpan,
                    jsonNode["items"]!,
                    context
                ).GetReferenceJsonNode();

                jsonNode.WriteTo(jsonWriter);
            }
            else if (isString)
            {
                if (jsonNode["enum"] is { } jsonNodeEnum)
                {
                    var enumValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var enumValue in jsonNodeEnum.AsArray())
                    {
                        enumValues.Add(enumValue!.GetValue<string>());
                    }

                    jsonNode = context.GetEnumComponentReference(
                        component.PropertyName!.AsSpan(),
                        lastPropertySpan,
                        enumValues
                    ).GetReferenceJsonNode();
                }

                jsonNode.WriteTo(jsonWriter);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        return false;
    }

    private static string ProcessItemInternal(
        ReadOnlySpan<char> typePrefix,
        ReadOnlySpan<char> lastPropertySpan,
        JsonNode jsonNode,
        TransposeContext context
    )
    {
        if (
            TryProcessItemInternal(
                typePrefix,
                lastPropertySpan,
                jsonNode,
                context,
                out var reference
            )
        )
        {
            return reference;
        }

        throw new Exception(
            $"Unable to process item {typePrefix.ToString()}.{lastPropertySpan.ToString()}"
        );
    }

    private static bool TryProcessItemInternal(
        ReadOnlySpan<char> typePrefix,
        ReadOnlySpan<char> lastPropertySpan,
        JsonNode jsonNode,
        TransposeContext context,
        [NotNullWhen(true)] out string? reference
    )
    {
        var titleSpan = typePrefix.CreateTypeName(lastPropertySpan);

        if (jsonNode["properties"] is { } innerProperties)
        {
            foreach (var innerProperty in innerProperties.AsObject().ToList())
            {
                if (innerProperty.Value is { } subProperty)
                {
                    if (
                        subProperty["type"]?.GetValue<string>() == "object"
                        && subProperty["properties"] is not null
                    )
                    {
                        innerProperties[innerProperty.Key] = ProcessItemInternal(
                            titleSpan,
                            innerProperty.Key.AsSpan(),
                            subProperty,
                            context
                        ).GetReferenceJsonNode();
                    }
                    else if (
                        subProperty["type"]?.GetValue<string>() == "array"
                        && subProperty["items"] is { } itemsJsonNode
                        && itemsJsonNode["$ref"] is null
                    )
                    {
                        if (
                            TryProcessItemInternal(
                                titleSpan,
                                innerProperty.Key.AsSpan(),
                                itemsJsonNode,
                                context,
                                out var innerReference
                            )
                        )
                        {
                            subProperty["items"] = innerReference.GetReferenceJsonNode();
                        }
                    }
                    else if (
                        subProperty["type"]?.GetValue<string>() == "string"
                        && subProperty["enum"] is { } enumValuesJsonNode
                    )
                    {
                        var enumValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var enumValue in enumValuesJsonNode.AsArray())
                        {
                            enumValues.Add(enumValue!.GetValue<string>());
                        }

                        innerProperties[innerProperty.Key] = context.GetEnumComponentReference(
                            titleSpan,
                            innerProperty.Key.AsSpan(),
                            enumValues
                        ).GetReferenceJsonNode();
                    }
                }
            }
        }
        else if (
            jsonNode["type"]?.GetValue<string>() == "array"
            && jsonNode["items"] is { } itemsJsonNode
            && itemsJsonNode["$ref"] is null
        )
        {
            jsonNode["items"] = ProcessItemInternal(
                titleSpan,
                ReadOnlySpan<char>.Empty,
                itemsJsonNode,
                context
            ).GetReferenceJsonNode();

            reference = null;
            return false;
        }

        reference = context.AddComponent(
            titleSpan.ToString(),
            jsonNode
        );

        return true;
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
            foreach (var renamedComponentValue in context.NewComponents)
            {
                jsonWriter.WritePropertyName(renamedComponentValue.Key);
                renamedComponentValue.Value.WriteTo(jsonWriter);
            }

            return true;
        }

        return false;
    }
}
