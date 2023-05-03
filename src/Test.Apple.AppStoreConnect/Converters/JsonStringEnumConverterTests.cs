using Apple.AppStoreConnect.Converters;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;
using Xunit;

namespace Test.Apple.AppStoreConnect.Converters;

public class JsonStringEnumConverterTests
{
    [Theory]
    [MemberData(nameof(Data))]
    public void DeserializeWorks(MyEnum expectedEnumValue, string jsonValue)
    {
        using var loggerFactory = new LoggerFactory();

        var enumValue = JsonSerializer.Deserialize<MyEnum>(
            jsonValue,
            new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverterFactory(),
                }
            }
        );

        Assert.Equal(expectedEnumValue, enumValue);
    }


    [Theory]
    [MemberData(nameof(Data))]
    public void SerializeWorks(MyEnum enumValue, string expectedJsonValue)
    {
        using var loggerFactory = new LoggerFactory();

        var jsonValue = JsonSerializer.Serialize(
            enumValue,
            new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverterFactory(),
                }
            }
        );

        Assert.Equal(expectedJsonValue, jsonValue);
    }

    public static IEnumerable<object[]> Data()
    {
        yield return new object[]
        {
            MyEnum.App,
            "\"app\"",
        };
        yield return new object[]
        {
            MyEnum.Territory,
            "\"territory\""
        };
    }

    public enum MyEnum
    {
        [EnumMember(Value = "app")]
        App = 0,

        [EnumMember(Value = "territory")]
        Territory = 1,
    }
}
