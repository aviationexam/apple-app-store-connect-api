using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apple.AppStoreConnect.Converters;

public class OneOfJsonConverter<TOneOf> : JsonConverter<TOneOf>
    where TOneOf : OneOf
{
    public override TOneOf Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, TOneOf value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
