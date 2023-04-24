using System;
using System.Collections.Generic;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public sealed class TransposeContext
{
    private readonly IDictionary<string, IReadOnlyCollection<string>> _enumComponentValues =
        new Dictionary<string, IReadOnlyCollection<string>>();

    public string GetEnumComponentReference(
        string tag,
        ReadOnlySpan<char> parameterName, IReadOnlyCollection<string> enumValues
    )
    {
        var componentSchema = GetComponentName(tag, parameterName);

        if (!_enumComponentValues.ContainsKey(componentSchema))
        {
            _enumComponentValues.Add(componentSchema, enumValues);
        }

        return $"#/components/schemas/{componentSchema}";
    }

    private static string GetComponentName(
        string tag,
        ReadOnlySpan<char> parameterName
    )
    {
        var openingBracket = parameterName.IndexOf('[');

        if (openingBracket > 0)
        {
            parameterName = parameterName[openingBracket..];

            var closingBracket = parameterName.IndexOf(']');

            if (closingBracket > 0)
            {
                parameterName = parameterName[..closingBracket];
            }
        }

        return $"{tag}Parameter{parameterName.ToString()}";
    }
}
