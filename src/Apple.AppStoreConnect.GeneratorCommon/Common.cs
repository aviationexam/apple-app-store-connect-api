using System;

namespace Apple.AppStoreConnect.GeneratorCommon;

public static class Common
{
    public static ReadOnlySpan<byte> Utf8Bom => new byte[] { 0xEF, 0xBB, 0xBF };
}
