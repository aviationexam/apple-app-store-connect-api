using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apple.AppStoreConnect.Converters;

public class JsonStringEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    public override JsonConverter CreateConverter(
        Type typeToConvert, JsonSerializerOptions options
    ) => (JsonConverter) Activator.CreateInstance(
        typeof(JsonStringEnumConverter<>).MakeGenericType(typeToConvert),
        BindingFlags.Instance | BindingFlags.Public,
        binder: null,
        args: null,
        culture: null
    )!;
}
