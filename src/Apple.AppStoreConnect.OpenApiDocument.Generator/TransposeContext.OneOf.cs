using Apple.AppStoreConnect.GeneratorCommon.Extensions;
using Apple.AppStoreConnect.OpenApiDocument.Generator.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public sealed partial class TransposeContext
{
    private const string OneOfParentComponent = "OneOf";

    public string GetOneOfComponentReference(
        ReadOnlySpan<char> typePrefix,
        ReadOnlySpan<char> lastPropertySpan,
        IReadOnlyCollection<string> references
    )
    {
        if (!_newComponents.ContainsKey(OneOfParentComponent))
        {
            AddOneOfComponent();
        }

        var componentSchemaSpan = typePrefix.CreateTypeName(lastPropertySpan);
        var componentSchema = componentSchemaSpan.ToString();

        var enumTypeReference = GetEnumComponentReference(
            componentSchemaSpan,
            "Enum".AsSpan(),
            references.Select(x => x.GetComponentName().ToString()).ToList()
        );

        using (var memoryStream = new MemoryStream())
        {
            _jsonWriter.Reset(memoryStream);

            _jsonWriter.WriteStartObject();

            _jsonWriter.WritePropertyName("title"u8);
            _jsonWriter.WriteStringValue(componentSchema);

            _jsonWriter.WritePropertyName("allOf"u8);
            _jsonWriter.WriteStartArray();

            {
                _jsonWriter.WriteStartObject();

                _jsonWriter.WritePropertyName("$ref"u8);
                _jsonWriter.WriteStringValue(OneOfParentComponent.GetReferenceName());

                _jsonWriter.WriteEndObject();
            }
            {
                _jsonWriter.WriteStartObject();

                _jsonWriter.WritePropertyName("type"u8);
                _jsonWriter.WriteStringValue("object"u8);

                _jsonWriter.WritePropertyName("additionalProperties"u8);
                _jsonWriter.WriteBooleanValue(false);

                _jsonWriter.WritePropertyName("properties"u8);
                {
                    _jsonWriter.WriteStartObject();

                    _jsonWriter.WritePropertyName("OneOfType"u8);
                    _jsonWriter.WriteStartObject();

                    _jsonWriter.WritePropertyName("$ref"u8);
                    _jsonWriter.WriteStringValue(enumTypeReference);

                    _jsonWriter.WritePropertyName("nullable"u8);
                    _jsonWriter.WriteBooleanValue(false);

                    _jsonWriter.WriteEndObject();

                    foreach (var reference in references)
                    {
                        _jsonWriter.WritePropertyName(reference.GetComponentName());
                        _jsonWriter.WriteStartObject();

                        _jsonWriter.WritePropertyName("$ref"u8);
                        _jsonWriter.WriteStringValue(reference);

                        _jsonWriter.WritePropertyName("nullable"u8);
                        _jsonWriter.WriteBooleanValue(true);

                        _jsonWriter.WriteEndObject();
                    }

                    _jsonWriter.WriteEndObject();
                }
                _jsonWriter.WriteEndObject();
            }

            _jsonWriter.WriteEndArray();

            _jsonWriter.WriteEndObject();

            _jsonWriter.Flush();

            memoryStream.Seek(0, SeekOrigin.Begin);
            var jsonNode = JsonNode.Parse(memoryStream)!;

            AddComponent(componentSchema, jsonNode);
        }

        _jsonWriter.Reset(Stream.Null);

        return componentSchema.GetReferenceName();
    }

    private void AddOneOfComponent()
    {
        using var memoryStream = new MemoryStream();
        _jsonWriter.Reset(memoryStream);

        _jsonWriter.WriteStartObject();

        _jsonWriter.WritePropertyName("type"u8);
        _jsonWriter.WriteStringValue("object"u8);

        _jsonWriter.WritePropertyName("title"u8);
        _jsonWriter.WriteStringValue(OneOfParentComponent);

        _jsonWriter.WritePropertyName("x-abstract"u8);
        _jsonWriter.WriteBooleanValue(true);

        _jsonWriter.WritePropertyName("additionalProperties"u8);
        _jsonWriter.WriteBooleanValue(false);

        _jsonWriter.WriteEndObject();

        _jsonWriter.Flush();

        memoryStream.Seek(0, SeekOrigin.Begin);
        var jsonNode = JsonNode.Parse(memoryStream)!;

        AddComponent(OneOfParentComponent, jsonNode);
    }
}
