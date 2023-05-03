using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.GeneratorCommon.Extensions;

public static class PathItemStackExtensions
{
    public static void WritePathItem(
        this Stack<PathItem> path,
        JsonTokenType tokenType,
        ref ReadOnlySpan<byte> lastProperty
    )
    {
        var parent = path.Count > 0 ? path.Peek() : null;

        if (!lastProperty.IsEmpty)
        {
            path.Push(new PathItem(tokenType, Encoding.UTF8.GetString(lastProperty.ToArray()), parent));
            lastProperty = null;
        }
        else
        {
            path.Push(new PathItem(tokenType, null, parent));
        }
    }
}
