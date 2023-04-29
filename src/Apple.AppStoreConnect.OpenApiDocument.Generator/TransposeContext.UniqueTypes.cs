using System;
using System.Collections.Generic;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public sealed partial class TransposeContext
{
    public IReadOnlyDictionary<string, string> RenamedComponentValues => _renamedComponentValues;

    private readonly Dictionary<string, string> _renamedComponentValues = new();

    public string AddComponent(string typeNameSpan, string json)
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
