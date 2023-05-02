using Apple.AppStoreConnect.OpenApiDocument.Generator.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public sealed partial class TransposeContext
{
    private readonly Dictionary<string, IReadOnlyCollection<string>> _enumComponentValues = new();

    public string GetEnumComponentReference(
        ReadOnlySpan<char> typePrefix,
        ReadOnlySpan<char> lastPropertySpan,
        IReadOnlyCollection<string> enumValues
    )
    {
        var componentSchema = typePrefix.CreateTypeName(lastPropertySpan).ToString();

        if (_enumComponentValues.TryGetValue(componentSchema, out var previousEnums))
        {
            if (previousEnums.SequenceEqual(enumValues.OrderBy(x => x)))
            {
                return GetReferenceName(componentSchema);
            }

            var i = 2;
            var componentSchemaCandidate = $"{componentSchema}{i}";

            while (_enumComponentValues.TryGetValue(componentSchemaCandidate, out previousEnums))
            {
                if (previousEnums.SequenceEqual(enumValues.OrderBy(x => x)))
                {
                    return GetReferenceName(componentSchemaCandidate);
                }

                i++;
                componentSchemaCandidate = $"{componentSchema}{i}";
            }

            componentSchema = componentSchemaCandidate;
        }

        using (var memoryStream = new MemoryStream())
        {
            _jsonWriter.Reset(memoryStream);

            _jsonWriter.WriteStartObject();

            _jsonWriter.WritePropertyName("type"u8);
            _jsonWriter.WriteStringValue("string"u8);

            _jsonWriter.WritePropertyName("enum"u8);
            _jsonWriter.WriteStartArray();

            foreach (var enumValue in enumValues)
            {
                _jsonWriter.WriteStringValue(enumValue);
            }

            _jsonWriter.WriteEndArray();

            _jsonWriter.WriteEndObject();

            _jsonWriter.Flush();

            memoryStream.Seek(0, SeekOrigin.Begin);
            var jsonNode = JsonNode.Parse(memoryStream)!;

            _enumComponentValues.Add(componentSchema, enumValues.OrderBy(x => x).ToList());
            _newComponents.Add(componentSchema, jsonNode);
        }

        _jsonWriter.Reset(Stream.Null);

        return GetReferenceName(componentSchema);
    }
}
