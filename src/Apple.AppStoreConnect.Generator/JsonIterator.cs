using Apple.AppStoreConnect.GeneratorCommon;
using Apple.AppStoreConnect.GeneratorCommon.Extensions;
using H.Generators;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Apple.AppStoreConnect.Generator;

public static class JsonIterator
{
    public static IEnumerable<FileWithName> ProcessJson(
        ReadOnlySpan<byte> jsonReadOnlySpan
    )
    {
        var documentOptions = new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
        };

        var jsonReader = new Utf8JsonReader(jsonReadOnlySpan, documentOptions);

        var path = new Stack<PathItem>();
        ReadOnlySpan<byte> lastProperty = null;

        while (jsonReader.Read())
        {
            var tokenType = jsonReader.TokenType;

            switch (tokenType)
            {
                case JsonTokenType.StartObject:
                    path.WritePathItem(tokenType, ref lastProperty);

                    break;
                case JsonTokenType.EndObject:
                    PathItem? pathItem = null;
                    if (path.Count > 0)
                    {
                        pathItem = path.Pop();
                    }

                    lastProperty = default;
                    break;
                case JsonTokenType.StartArray:
                    path.WritePathItem(tokenType, ref lastProperty);

                    break;
                case JsonTokenType.EndArray:
                    if (path.Count > 0)
                    {
                        path.Pop();
                    }

                    lastProperty = default;
                    break;
                case JsonTokenType.PropertyName:
                    if (jsonReader.HasValueSequence)
                    {
                        throw new Exception();
                    }

                    lastProperty = jsonReader.ValueSpan;

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

                    break;
                case JsonTokenType.Number:
                    break;
                case JsonTokenType.True:
                    break;
                case JsonTokenType.False:
                    break;
                case JsonTokenType.Null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        yield break;
    }
}
