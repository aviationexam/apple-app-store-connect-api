using System.Reflection;
using System.Text.Json;

namespace Apple.AppStoreConnect.Converters;

public interface ITypedWriter<TOneOf>
{
    void Write(
        TOneOf oneOf,
        PropertyInfo propertyInfo,
        Utf8JsonWriter writer, JsonSerializerOptions options
    );
}
