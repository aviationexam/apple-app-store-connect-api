using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apple.AppStoreConnect.Converters;

public class OneOfJsonConverterFactory : JsonConverterFactory
{
    private readonly Type _oneOfType = typeof(OneOf);

    public override bool CanConvert(Type typeToConvert) => _oneOfType.IsAssignableTo(typeToConvert);

    public override JsonConverter CreateConverter(
        Type typeToConvert, JsonSerializerOptions options
    ) => (JsonConverter) Activator.CreateInstance(
        typeof(OneOfJsonConverter<>).MakeGenericType(typeToConvert),
        BindingFlags.Instance | BindingFlags.Public,
        binder: null,
        args: new object[] { options },
        culture: null
    )!;
}
