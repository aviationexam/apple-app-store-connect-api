using Apple.AppStoreConnect.OpenApiDocument.Generator.Processors;
using System;
using System.Collections.Generic;
using System.Text;
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

        while (jsonReader.Read())
        {
            var tokenType = jsonReader.TokenType;

            switch (tokenType)
            {
                case JsonTokenType.StartObject:
                    WritePathItem(tokenType, path, ref lastProperty);

                    jsonWriter.WriteStartObject();
                    break;
                case JsonTokenType.EndObject:
                    if (path.Count > 0)
                    {
                        path.Pop();
                    }

                    jsonWriter.WriteEndObject();
                    break;
                case JsonTokenType.StartArray:
                    WritePathItem(tokenType, path, ref lastProperty);

                    jsonWriter.WriteStartArray();
                    break;
                case JsonTokenType.EndArray:
                    if (path.Count > 0)
                    {
                        path.Pop();
                    }

                    jsonWriter.WriteEndArray();
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
                        ref jsonReader, jsonWriter
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

    private static void WritePathItem(
        JsonTokenType tokenType,
        Stack<PathItem> path, ref ReadOnlySpan<byte> lastProperty
    )
    {
        if (!lastProperty.IsEmpty)
        {
            path.Push(new PathItem(tokenType, Encoding.UTF8.GetString(lastProperty.ToArray())));
            lastProperty = null;
        }
        else
        {
            path.Push(new PathItem(tokenType, null));
        }
    }

    private static bool TryProcessItems(
        IReadOnlyCollection<PathItem> path,
        ReadOnlySpan<byte> lastProperty,
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter
    ) => SubscriptionStatusUrlVersionProcessor.TryProcessItem(
        path,
        lastProperty,
        ref jsonReader, jsonWriter
    );
}
