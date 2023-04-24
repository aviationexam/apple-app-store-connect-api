using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public static class PathItemStackExtensions
{
    public static void WritePathItem(
        this Stack<PathItem> path,
        JsonTokenType tokenType,
        ref ReadOnlySpan<byte> lastProperty
    )
    {
        if (!lastProperty.IsEmpty)
        {
            path.Push(new PathItem(tokenType, Encoding.UTF8.GetString(lastProperty.ToArray())));
            lastProperty = null;
        }
        else
        {
            path.Push(new PathItem(tokenType, null));
        }
    }
}
