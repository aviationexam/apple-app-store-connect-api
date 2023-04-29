using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public sealed partial class TransposeContext
{
    public IReadOnlyDictionary<string, JsonNode> RenamedComponentValues => _renamedComponentValues;

    private readonly Dictionary<string, JsonNode> _renamedComponentValues = new();

    public string AddComponent(string typeNameSpan, JsonNode json)
    {
        if (!_renamedComponentValues.TryGetValue(typeNameSpan, out _))
        {
            _renamedComponentValues.Add(typeNameSpan, json);
        }
        else
        {
            throw new Exception($"Type {typeNameSpan} already exists");
        }

        return GetReferenceName(typeNameSpan);
    }
}
