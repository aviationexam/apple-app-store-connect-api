using Apple.AppStoreConnect.OpenApiDocument.Generator.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

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

        _enumComponentValues.Add(componentSchema, enumValues);

        return GetReferenceName(componentSchema);
    }

    public string GetEnumComponentReference(
        IReadOnlyCollection<PathItem> parentPath,
        string tag,
        ReadOnlySpan<char> parameterName,
        ReadOnlySpan<char> operationId,
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

            componentSchema = VerifyComponentSchema($"{tag}{httpMethod}{componentName}", enumValues);
        }

        if (
            componentSchema is null
            && operationId.IndexOf('-') is var dashIndex and > 0
        )
        {
            dashIndex++;
            operationId = operationId[dashIndex..];
            dashIndex = operationId.IndexOf('-');

            ReadOnlySpan<char> operationSuffix;
            if (dashIndex > 0)
            {
                operationSuffix = operationId[(dashIndex + 1)..];
                operationId = operationId[..dashIndex];
            }
            else
            {
                operationSuffix = operationId;
                operationId = default;
            }

            if (
                operationId.Length > 0
                && char.IsLower(operationId[0])
            )
            {
                var operationIdSpan = operationId.ToArray().AsSpan();
                operationIdSpan[0] = char.ToUpperInvariant(operationIdSpan[0]);

                operationId = operationIdSpan;
            }

            if (
                !operationId.IsEmpty
                && operationId.Length > tag.Length
                && !operationId.SequenceEqual("AppPricePoints".AsSpan())
            )
            {
                if (operationId[..(tag.Length - 1)].SequenceEqual(tag.AsSpan()[..^1]))
                {
                    operationId = operationId[(tag.Length - 1)..];
                }
            }

            var i = 0;
            Span<char> operationName = new char[operationSuffix.Length];
            while (operationSuffix.IndexOf('_') is var underscoreIndex and > 0)
            {
                var token = operationSuffix[..underscoreIndex];
                token.CopyTo(operationName[i..]);

                if (char.IsLower(operationName[i]))
                {
                    operationName[i] = char.ToUpperInvariant(operationName[i]);
                }

                i += token.Length;
                operationSuffix = operationSuffix[(underscoreIndex + 1)..];
            }

            operationSuffix.CopyTo(operationName[i..]);
            if (char.IsLower(operationName[i]))
            {
                operationName[i] = char.ToUpperInvariant(operationName[i]);
            }

            i += operationSuffix.Length;
            operationName = operationName[..i];

            componentSchema = VerifyComponentSchema(
                $"{tag}{operationId.ToString()}{operationName.ToString()}{componentName}",
                enumValues
            );
        }

        if (componentSchema is null)
        {
            throw new NullReferenceException($"{nameof(componentSchema)} should not be null");
        }

        return GetReferenceName(componentSchema);
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
