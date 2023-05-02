using Apple.AppStoreConnect;
using Apple.AppStoreConnect.Converters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Test.Apple.AppStoreConnect.Converters;

public class OneOfJsonConverterTests
{
    [Theory]
    [MemberData(nameof(OneOfData))]
    public void DeserializeOneOfWorks(string json)
    {
        using var loggerFactory = new LoggerFactory();

        var parsedBody = JsonSerializer.Deserialize<TestingAppAvailabilityResponseIncluded>(
            json,
            new JsonSerializerOptions
            {
                Converters =
                {
                    new OneOfJsonConverterFactory(loggerFactory),
                }
            }
        );

        Assert.NotNull(parsedBody);
        Assert.NotNull(parsedBody.OneOfType);

        switch (parsedBody.OneOfType)
        {
            case AppAvailabilityResponseIncludedEnum.App:
                Assert.NotNull(parsedBody.App);
                Assert.Null(parsedBody.Territory);

                Assert.Equal(AppType.Apps, parsedBody.App.Type);
                Assert.Equal("app-id", parsedBody.App.Id);
                break;
            case AppAvailabilityResponseIncludedEnum.Territory:
                Assert.NotNull(parsedBody.Territory);
                Assert.Null(parsedBody.App);

                Assert.Equal(TerritoryType.Territories, parsedBody.Territory.Type);
                Assert.Equal("territory-id", parsedBody.Territory.Id);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(parsedBody.OneOfType), parsedBody.OneOfType,
                    $"Unexpected {nameof(parsedBody.OneOfType)} '{parsedBody.OneOfType}'"
                );
        }
    }

    public static IEnumerable<object[]> OneOfData()
    {
        yield return new object[]
        {
            """
            {
                "type": "apps",
                "id": "app-id"
            }
            """,
        };
        yield return new object[]
        {
            """
            {
                "type": "territories",
                "id": "territory-id"
            }
            """,
        };
    }

    private enum AppAvailabilityResponseIncludedEnum
    {
        [EnumMember(Value = @"App")]
        App = 0,

        [EnumMember(Value = @"Territory")]
        Territory = 1,
    }

    private record TestingAppAvailabilityResponseIncluded : OneOf
    {
        [JsonPropertyName("OneOfType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AppAvailabilityResponseIncludedEnum? OneOfType { get; set; }

        [JsonPropertyName("App")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public App? App { get; set; }

        [JsonPropertyName("Territory")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Territory? Territory { get; set; }
    }
}
