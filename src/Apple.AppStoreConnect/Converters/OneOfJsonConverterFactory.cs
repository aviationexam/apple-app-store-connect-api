using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apple.AppStoreConnect.Converters;

public class OneOfJsonConverterFactory(
    ILoggerFactory loggerFactory
) : JsonConverterFactory
{
    private readonly Type _oneOfType = typeof(OneOf);

    public override bool CanConvert(Type typeToConvert) => _oneOfType.IsAssignableFrom(typeToConvert);

    public override JsonConverter CreateConverter(
        Type typeToConvert, JsonSerializerOptions options
    ) => (JsonConverter) Activator.CreateInstance(
        typeof(OneOfJsonConverter<>).MakeGenericType(typeToConvert),
        BindingFlags.Instance | BindingFlags.Public,
        binder: null,
        args: [loggerFactory.CreateLogger(typeToConvert)],
        culture: null
    )!;
}
