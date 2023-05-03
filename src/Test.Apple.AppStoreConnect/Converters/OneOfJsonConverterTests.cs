using Apple.AppStoreConnect;
using Apple.AppStoreConnect.Converters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;
using Xunit;

namespace Test.Apple.AppStoreConnect.Converters;

public class OneOfJsonConverterTests
{
    [Theory]
    [MemberData(nameof(OneOfJsonData))]
    public void DeserializeOneOfWorks(string json)
    {
        using var loggerFactory = new LoggerFactory();

        var parsedBody = JsonSerializer.Deserialize<TestingAppAvailabilityResponseIncluded>(
            json,
            new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverterFactory(),
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

    [Theory]
    [MemberData(nameof(OneOfObjectData))]
    public void SerializeOneOfWorks(TestingAppAvailabilityResponseIncluded oneOf)
    {
        using var loggerFactory = new LoggerFactory();

        var json = JsonSerializer.Serialize(
            oneOf,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverterFactory(),
                    new OneOfJsonConverterFactory(loggerFactory),
                }
            }
        );

        Assert.NotNull(json);

        switch (oneOf.OneOfType)
        {
            case AppAvailabilityResponseIncludedEnum.App:
                Assert.Equal("""
                {
                  "type": "apps",
                  "id": "app-id",
                  "links": {
                    "self": "self-url"
                  }
                }
                """, json);
                break;
            case AppAvailabilityResponseIncludedEnum.Territory:
                Assert.Equal("""
                {
                  "type": "territories",
                  "id": "territory-id",
                  "links": {
                    "self": "self-url"
                  }
                }
                """, json);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(oneOf.OneOfType), oneOf.OneOfType,
                    $"Unexpected {nameof(oneOf.OneOfType)} '{oneOf.OneOfType}'"
                );
        }
    }

    public static IEnumerable<object[]> OneOfJsonData()
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

    public static IEnumerable<object[]> OneOfObjectData()
    {
        yield return new object[]
        {
            new TestingAppAvailabilityResponseIncluded
            {
                OneOfType = AppAvailabilityResponseIncludedEnum.App,
                App = new App
                {
                    Type = AppType.Apps,
                    Id = "app-id",
                    Links = new ResourceLinks
                    {
                        Self = "self-url",
                    },
                }
            },
        };
        yield return new object[]
        {
            new TestingAppAvailabilityResponseIncluded
            {
                OneOfType = AppAvailabilityResponseIncludedEnum.Territory,
                Territory = new Territory
                {
                    Type = TerritoryType.Territories,
                    Id = "territory-id",
                    Links = new ResourceLinks
                    {
                        Self = "self-url",
                    },
                }
            },
        };
    }

    public enum AppAvailabilityResponseIncludedEnum
    {
        [EnumMember(Value = @"App")]
        App = 0,

        [EnumMember(Value = @"Territory")]
        Territory = 1,
    }

    public record TestingAppAvailabilityResponseIncluded : OneOf
    {
        public AppAvailabilityResponseIncludedEnum? OneOfType { get; set; }

        public App? App { get; set; }

        public Territory? Territory { get; set; }
    }
}
