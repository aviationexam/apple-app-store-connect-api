using System.Reflection;
using System.Text.Json;

namespace Apple.AppStoreConnect.Converters;

public interface ITypedReader<TOneOf>
{
    void Read(
        TOneOf oneOf,
        PropertyInfo propertyInfo,
        ref Utf8JsonReader reader, JsonSerializerOptions options
    );
}
