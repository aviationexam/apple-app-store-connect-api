using Apple.AppStoreConnect.GeneratorCommon.Extensions;
using System;
using System.Text.Json.Nodes;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public sealed partial class TransposeContext
{
    public string AddComponent(string typeNameSpan, JsonNode json)
    {
        if (!_newComponents.TryGetValue(typeNameSpan, out _))
        {
            _newComponents.Add(typeNameSpan, json);
        }
        else
        {
            throw new Exception($"Type {typeNameSpan} already exists");
        }

        return typeNameSpan.GetReferenceName();
    }
}
