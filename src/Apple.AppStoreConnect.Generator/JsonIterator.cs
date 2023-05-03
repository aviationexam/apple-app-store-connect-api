using Apple.AppStoreConnect.Generator.Processors;
using Apple.AppStoreConnect.GeneratorCommon;
using Apple.AppStoreConnect.GeneratorCommon.Extensions;
using H.Generators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.Generator;

public static class JsonIterator
{
    public static IEnumerable<FileWithName> ProcessJson(
        ref Utf8JsonReader jsonReader,
        ReadOnlySpan<char> targetNamespace
    )
    {
        var path = new Stack<PathItem>();
        ReadOnlySpan<byte> lastProperty = null;
        var resultFileNames = new List<FileWithName>();

        while (jsonReader.Read())
        {
            var tokenType = jsonReader.TokenType;

            var a = Encoding.UTF8.GetString(jsonReader.ValueSpan.ToArray());

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

                    var fileWithNames = TryProcessItems(
                        targetNamespace,
                        path, lastProperty,
                        ref jsonReader
                    );

                    foreach (var fileWithName in fileWithNames)
                    {
                        if (!fileWithName.IsEmpty)
                        {
                            resultFileNames.Add(fileWithName);
                        }
                    }

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

        return resultFileNames;
    }

    private static IReadOnlyCollection<FileWithName> TryProcessItems(
        ReadOnlySpan<char> targetNamespace,
        IReadOnlyCollection<PathItem> path,
        ReadOnlySpan<byte> lastProperty,
        ref Utf8JsonReader jsonReader
    )
    {
        var getNextFileWithName = GetNextProcessor.TryProcessItems(
            targetNamespace,
            path, lastProperty,
            ref jsonReader
        );

        return new[]
        {
            getNextFileWithName,
        };
    }
}
