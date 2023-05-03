using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apple.AppStoreConnect.Converters;

public readonly struct TypedReader<TOneOf, TTargetType> : ITypedReader<TOneOf>
    where TOneOf : OneOf
    where TTargetType : class
{
    public void Read(
        TOneOf oneOf,
        PropertyInfo propertyInfo,
        ref Utf8JsonReader reader, JsonSerializerOptions options
    )
    {
        var targetConverter = (JsonConverter<TTargetType>) options.GetConverter(propertyInfo.PropertyType);

        var innerTypeValue = targetConverter.Read(ref reader, propertyInfo.PropertyType, options);

        propertyInfo.SetValue(oneOf, innerTypeValue);
    }
}
