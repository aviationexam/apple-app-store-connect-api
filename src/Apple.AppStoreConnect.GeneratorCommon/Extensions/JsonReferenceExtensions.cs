using System;

namespace Apple.AppStoreConnect.GeneratorCommon.Extensions;

public static class JsonReferenceExtensions
{
    public static string GetReferenceName(
        this string componentSchema
    ) => $"#/components/schemas/{componentSchema}";

    public static ReadOnlySpan<char> GetComponentName(
        this string reference
    )
    {
        var referenceSpan = reference.AsSpan();

        var slashIndex = referenceSpan.LastIndexOf('/');
        slashIndex++;

        return referenceSpan[slashIndex..];
    }
}
