using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public partial class TransposeContext : IDisposable
{
    private readonly Utf8JsonWriter _jsonWriter = new(Stream.Null);

    public void Dispose()
    {
        _jsonWriter.Dispose();
    }

    public IReadOnlyDictionary<string, JsonNode> NewComponents => _newComponents;

    private readonly Dictionary<string, JsonNode> _newComponents = new();
}
