using System;

namespace Apple.AppStoreConnect.Extensions;

public static class TypeExtensions
{
    public static Type GetNotNullableType(
        this Type type
    )
    {
        if (
            type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(Nullable<>)
        )
        {
            return type.GenericTypeArguments[0];
        }

        return type;
    }
}
