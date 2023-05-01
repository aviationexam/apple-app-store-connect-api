using Apple.AppStoreConnect.OpenApiDocument.Generator.Extensions;
using Apple.AppStoreConnect.OpenApiDocument.Generator.Processors;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public static class JsonIterator
{
    public static void ProcessJson(
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter
    )
    {
        var path = new Stack<PathItem>();
        ReadOnlySpan<byte> lastProperty = null;
        var context = new TransposeContext();

        while (jsonReader.Read())
        {
            var tokenType = jsonReader.TokenType;

            switch (tokenType)
            {
                case JsonTokenType.StartObject:
                    path.WritePathItem(tokenType, ref lastProperty);

                    jsonWriter.WriteStartObject();
                    break;
                case JsonTokenType.EndObject:
                    PathItem? pathItem = null;
                    if (path.Count > 0)
                    {
                        pathItem = path.Pop();
                    }

                    TryWriteAdditional(
                        pathItem,
                        path,
                        jsonWriter,
                        context
                    );

                    jsonWriter.WriteEndObject();
                    lastProperty = default;
                    break;
                case JsonTokenType.StartArray:
                    path.WritePathItem(tokenType, ref lastProperty);

                    jsonWriter.WriteStartArray();
                    break;
                case JsonTokenType.EndArray:
                    if (path.Count > 0)
                    {
                        path.Pop();
                    }

                    jsonWriter.WriteEndArray();
                    lastProperty = default;
                    break;
                case JsonTokenType.PropertyName:
                    if (jsonReader.HasValueSequence)
                    {
                        throw new Exception();
                    }

                    lastProperty = jsonReader.ValueSpan;

                    jsonWriter.WritePropertyName(jsonReader.ValueSpan);

                    TryProcessItems(
                        path, lastProperty,
                        ref jsonReader, jsonWriter,
                        context
                    );
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

                    path.Peek().AddUsefulProperty(lastProperty, jsonReader.ValueSpan);

                    jsonWriter.WriteStringValue(jsonReader.ValueSpan);
                    break;
                case JsonTokenType.Number:
                    jsonWriter.WriteNumberValue(jsonReader.GetInt32());
                    break;
                case JsonTokenType.True:
                    jsonWriter.WriteBooleanValue(true);
                    break;
                case JsonTokenType.False:
                    jsonWriter.WriteBooleanValue(false);
                    break;
                case JsonTokenType.Null:
                    jsonWriter.WriteNullValue();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
    private static bool TryProcessItems(
        IReadOnlyCollection<PathItem> path,
        ReadOnlySpan<byte> lastProperty,
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter,
        TransposeContext context
    ) => SubscriptionStatusUrlVersionProcessor.TryProcessItem(
        path,
        lastProperty,
        ref jsonReader, jsonWriter
    ) || AnonymousEnumProcessor.TryProcessItem(
        path,
        lastProperty,
        ref jsonReader, jsonWriter,
        context
    ) || AnonymousComplexPropertyProcessor.TryProcessItem(
        path,
        lastProperty,
        ref jsonReader, jsonWriter,
        context
    );

    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
    private static bool TryWriteAdditional(
        PathItem? pathItem,
        IReadOnlyCollection<PathItem> path,
        Utf8JsonWriter jsonWriter,
        TransposeContext context
    )
    {
        var response = AnonymousEnumProcessor.TryWriteAdditional(
            pathItem,
            path,
            jsonWriter,
            context
        );

        response = AnonymousComplexPropertyProcessor.TryWriteAdditional(
            pathItem,
            path,
            jsonWriter,
            context
        ) || response;

        return response;
    }
}
