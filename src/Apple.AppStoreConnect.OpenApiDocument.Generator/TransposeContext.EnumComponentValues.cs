using Apple.AppStoreConnect.OpenApiDocument.Generator.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public sealed partial class TransposeContext
{
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> EnumComponentValues => _enumComponentValues;

    private readonly Dictionary<string, IReadOnlyCollection<string>> _enumComponentValues = new();

    public string GetReferenceName(string componentSchema) => $"#/components/schemas/{componentSchema}";

    public string GetEnumComponentReference(
        ReadOnlySpan<char> typePrefix,
        ReadOnlySpan<char> lastPropertySpan,
        IReadOnlyCollection<string> enumValues
    )
    {
        var componentSchema = typePrefix.CreateTypeName(lastPropertySpan).ToString();

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

            memoryStream.Seek(0, SeekOrigin.Begin);
            var jsonNode = JsonNode.Parse(memoryStream)!;

            _newComponents.Add(componentSchema, jsonNode);
        }

        return GetReferenceName(componentSchema);
    }
}
