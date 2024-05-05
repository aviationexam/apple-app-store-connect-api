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

        if (titleSpan.Length > typePrefix.Length)
        {
            lastPropertySpan.CopyTo(titleSpan[componentNameLength..]);

            if (char.IsLower(titleSpan[componentNameLength]))
            {
                titleSpan[componentNameLength] = char.ToUpperInvariant(titleSpan[componentNameLength]);
            }
        }

        var i = titleSpan.IndexOf('.');
        while (i > -1)
        {
            titleSpan[(i + 1)..].CopyTo(titleSpan[i..]);
            titleSpan = titleSpan[..^1];

            if (char.IsLower(titleSpan[i]))
            {
                titleSpan[i] = char.ToUpperInvariant(titleSpan[i]);
            }

            i = titleSpan.IndexOf('.');
        }

        return titleSpan;
    }
}
