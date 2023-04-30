using System.Text.Json.Nodes;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator.Extensions;

public static class JsonNodeExtensions
{
    public static JsonNode GetReferenceJsonNode(this string referenceName) => JsonNode.Parse($$"""
    {
      "$ref": "{{referenceName}}"
    }
    """)!;
}
