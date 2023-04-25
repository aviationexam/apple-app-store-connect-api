using System;
using System.Collections.Generic;
using System.Linq;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public sealed class TransposeContext
{
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> EnumComponentValues => _enumComponentValues;

    private readonly Dictionary<string, IReadOnlyCollection<string>> _enumComponentValues = new();

    public string GetEnumComponentReference(
        IReadOnlyCollection<PathItem> parentPath,
        string tag,
        ReadOnlySpan<char> parameterName,
        IReadOnlyCollection<string> enumValues
    )
    {
        var componentName = GetComponentName(parameterName);

        var componentSchema = VerifyComponentSchema($"{tag}{componentName}", enumValues);
        if (
            componentSchema is null
            && parentPath.Count > 2
            && parentPath.ElementAt(2) is { PropertyName: { } httpMethod })
        {
            if (char.IsLower(httpMethod[0]))
            {
                var httpMethodSpan = httpMethod.ToArray().AsSpan();

                httpMethodSpan[0] = char.ToUpperInvariant(httpMethod[0]);
                httpMethod = httpMethodSpan.ToString();
            }

            var name = $"{tag}{httpMethod}{componentName}";
            componentSchema = VerifyComponentSchema(name, enumValues);
            var i = 2;
            while (componentSchema is null)
            {
                componentSchema = VerifyComponentSchema($"{name}{i}", enumValues);
                i++;
            }
        }

        return $"#/components/schemas/{componentSchema}";
    }

    private string? VerifyComponentSchema(string componentSchema, IReadOnlyCollection<string> enumValues)
    {
        if (_enumComponentValues.TryGetValue(componentSchema, out var existingEnumValues))
        {
            if (enumValues.OrderBy(x => x).SequenceEqual(existingEnumValues))
            {
                return componentSchema;
            }
        }
        else
        {
            _enumComponentValues.Add(componentSchema, enumValues.OrderBy(x => x).ToList());
            return componentSchema;
        }

        return null;
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
