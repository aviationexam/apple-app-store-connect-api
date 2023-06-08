using Apple.AppStoreConnect.GeneratorCommon;
using Apple.AppStoreConnect.GeneratorCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator.Processors;

public static class AnonymousEnumParameterProcessor
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
                && path.ElementAt(2).Properties.TryGetValue("operationId", out var operationId)
                && TryReadInner(path, name, operationId, ref jsonReaderClone, jsonWriter, context)
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

    private static bool TryReadInner(
        IReadOnlyCollection<PathItem> parentPath,
        string name, string operationId,
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter,
        TransposeContext context
    )
    {
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

        var componentPrefix = GetComponentPrefix(
            parentPath,
            name.AsSpan(),
            operationId.AsSpan()
        );

        var reference = context.GetEnumComponentReference(
            componentPrefix,
            ReadOnlySpan<char>.Empty,
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

    private static ReadOnlySpan<char> GetComponentPrefix(
        IReadOnlyCollection<PathItem> parentPath,
        ReadOnlySpan<char> parameterName,
        ReadOnlySpan<char> operationId
    )
    {
        var componentName = GetComponentName(parameterName);

        var urlPath = parentPath.ElementAt(3).PropertyName!.ToArray().AsSpan();
        if (urlPath[0] == '/')
        {
            urlPath = urlPath[1..];
        }

        var slashIndex = urlPath.IndexOf('/');
        var urlVersion = urlPath[..slashIndex];

        if (char.IsLower(urlVersion[0]))
        {
            urlVersion[0] = char.ToUpperInvariant(urlVersion[0]);
        }

        if (operationId.IndexOf('-') is var dashIndex and > 0)
        {
            var operationGroupName = operationId[..dashIndex];
            dashIndex++;
            operationId = operationId[dashIndex..];
            dashIndex = operationId.IndexOf('-');

            ReadOnlySpan<char> operationSuffix;
            if (dashIndex > 0)
            {
                operationSuffix = operationId[(dashIndex + 1)..];
                operationId = operationId[..dashIndex];
            }
            else
            {
                operationSuffix = operationId;
                operationId = default;
            }

            if (
                operationGroupName.Length > 0
                && char.IsLower(operationGroupName[0])
            )
            {
                var operationGroupNameSpan = operationGroupName.ToArray().AsSpan();
                operationGroupNameSpan[0] = char.ToUpperInvariant(operationGroupNameSpan[0]);

                operationGroupName = operationGroupNameSpan;
            }

            if (
                operationId.Length > 0
                && char.IsLower(operationId[0])
            )
            {
                var operationIdSpan = operationId.ToArray().AsSpan();
                operationIdSpan[0] = char.ToUpperInvariant(operationIdSpan[0]);

                operationId = operationIdSpan;
            }

            var i = 0;
            Span<char> operationName = new char[operationSuffix.Length];
            while (operationSuffix.IndexOf('_') is var underscoreIndex and > 0)
            {
                var token = operationSuffix[..underscoreIndex];
                token.CopyTo(operationName[i..]);

                if (char.IsLower(operationName[i]))
                {
                    operationName[i] = char.ToUpperInvariant(operationName[i]);
                }

                i += token.Length;
                operationSuffix = operationSuffix[(underscoreIndex + 1)..];
            }

            operationSuffix.CopyTo(operationName[i..]);
            if (char.IsLower(operationName[i]))
            {
                operationName[i] = char.ToUpperInvariant(operationName[i]);
            }

            i += operationSuffix.Length;
            operationName = operationName[..i];

            return
                $"{operationGroupName.ToString()}{urlVersion.ToString()}Operation{operationId.ToString()}{operationName.ToString()}{componentName}"
                    .AsSpan();
        }

        return $"{urlVersion.ToString()}Operation{operationId.ToString()}{componentName}".AsSpan();
    }

    private static string GetComponentName(
        ReadOnlySpan<char> parameterName
    )
    {
        var openingBracket = parameterName.IndexOf('[');

        if (openingBracket > 0)
        {
            openingBracket++;
            parameterName = parameterName[openingBracket..];

            var closingBracket = parameterName.IndexOf(']');

            if (closingBracket > 0)
            {
                parameterName = parameterName[..closingBracket];
            }
        }

        if (char.IsLower(parameterName[0]))
        {
            var upper = new char[parameterName.Length].AsSpan();

            parameterName.CopyTo(upper);
            upper[0] = char.ToUpperInvariant(parameterName[0]);
            parameterName = upper;
        }

        return $"Parameter{parameterName.ToString()}";
    }
}
