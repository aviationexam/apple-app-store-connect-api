using System;

namespace Apple.AppStoreConnect.GeneratorCommon.Extensions;

public static class ByteSpanExtensions
{
    public static ReadOnlySpan<byte> TrimBom(this Span<byte> span)
    {
        if (span.StartsWith(Common.Utf8Bom))
        {
            return span[Common.Utf8Bom.Length..];
        }

        return span;
    }

    public static ReadOnlySpan<byte> TrimBom(this ReadOnlySpan<byte> span)
    {
        if (span.StartsWith(Common.Utf8Bom))
        {
            return span[Common.Utf8Bom.Length..];
        }

        return span;
    }
}
