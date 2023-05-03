using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apple.AppStoreConnect.Converters;

public readonly struct TypedWriter<TOneOf, TTargetType> : ITypedWriter<TOneOf>
    where TOneOf : OneOf
    where TTargetType : class
{
    public void Write(
        TOneOf oneOf,
        PropertyInfo propertyInfo,
        Utf8JsonWriter writer, JsonSerializerOptions options
    )
    {
        var innerValue = (TTargetType?) propertyInfo.GetValue(oneOf);

        if (innerValue is null)
        {
            writer.WriteNullValue();
            return;
        }

        var targetConverter = (JsonConverter<TTargetType>) options.GetConverter(propertyInfo.PropertyType);

        targetConverter.Write(writer, innerValue, options);
    }
}
