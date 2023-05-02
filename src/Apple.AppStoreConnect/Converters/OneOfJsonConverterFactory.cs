using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apple.AppStoreConnect.Converters;

public class OneOfJsonConverterFactory : JsonConverterFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Type _oneOfType = typeof(OneOf);

    public OneOfJsonConverterFactory(
        ILoggerFactory loggerFactory
    )
    {
        _loggerFactory = loggerFactory;
    }

    public override bool CanConvert(Type typeToConvert) => _oneOfType.IsAssignableFrom(typeToConvert);

    public override JsonConverter CreateConverter(
        Type typeToConvert, JsonSerializerOptions options
    ) => (JsonConverter) Activator.CreateInstance(
        typeof(OneOfJsonConverter<>).MakeGenericType(typeToConvert),
        BindingFlags.Instance | BindingFlags.Public,
        binder: null,
        args: new[] { _loggerFactory.CreateLogger(typeToConvert) },
        culture: null
    )!;
}
