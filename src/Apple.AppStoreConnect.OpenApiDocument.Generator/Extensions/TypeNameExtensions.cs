using System;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator.Extensions;

public static class TypeNameExtensions
{
    public static ReadOnlySpan<char> CreateTypeName(
        this ReadOnlySpan<char> typePrefix,
        ReadOnlySpan<char> lastPropertySpan
    )
    {
        var componentNameLength = typePrefix.Length;

        var titleSpan = new char[
            componentNameLength
            + lastPropertySpan.Length
        ].AsSpan();

        typePrefix.CopyTo(titleSpan);
        lastPropertySpan.CopyTo(titleSpan[componentNameLength..]);

        if (char.IsLower(titleSpan[componentNameLength]))
        {
            titleSpan[componentNameLength] = char.ToUpperInvariant(titleSpan[componentNameLength]);
        }

        return titleSpan;
    }
}
