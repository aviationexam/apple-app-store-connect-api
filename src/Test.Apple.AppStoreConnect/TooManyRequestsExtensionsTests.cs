using Apple.AppStoreConnect.Exceptions;
using Apple.AppStoreConnect.Extensions;
using Apple.AppStoreConnect.Models;
using System.Collections.Generic;
using Xunit;

namespace Test.Apple.AppStoreConnect;

public class TooManyRequestsExtensionsTests
{
    [Fact]
    public void TryParseLimitsWorks()
    {
        var dictionary = new Dictionary<string, IEnumerable<string>>
        {
            [TooManyRequestsException.RateLimitHeader] = new[] { "user-hour-lim:3500;user-hour-rem:500;" },
        };

        Assert.Equal(ETooManyRequestParseLimit.Success, dictionary.TryParseLimits(out var rateLimitResponse));

        Assert.Equal(3500, rateLimitResponse.UserHourLimit);
        Assert.Equal(500, rateLimitResponse.UserHourRemaining);
    }

    [Fact]
    public void TryParseLimitsWorks_Overused()
    {
        var dictionary = new Dictionary<string, IEnumerable<string>>
        {
            [TooManyRequestsException.RateLimitHeader] = new[] { "user-hour-lim:3500;user-hour-rem:-1;" },
        };

        Assert.Equal(ETooManyRequestParseLimit.Success, dictionary.TryParseLimits(out var rateLimitResponse));

        Assert.Equal(3500, rateLimitResponse.UserHourLimit);
        Assert.Equal(-1, rateLimitResponse.UserHourRemaining);
    }

    [Fact]
    public void TryParseLimitsFailed_MissingHeader()
    {
        var dictionary = new Dictionary<string, IEnumerable<string>>();

        Assert.Equal(ETooManyRequestParseLimit.MissingHeader, dictionary.TryParseLimits(out _));
    }

    [Fact]
    public void TryParseLimitsWorks_TooManyHttpHeaders()
    {
        var dictionary = new Dictionary<string, IEnumerable<string>>
        {
            [TooManyRequestsException.RateLimitHeader] = new[]
            {
                "user-hour-lim:3500;user-hour-rem:-1;",
                "user-hour-lim:3500;user-hour-rem:-1;",
            },
        };

        Assert.Equal(ETooManyRequestParseLimit.TooManyHttpHeaders, dictionary.TryParseLimits(out _));
    }

    [Fact]
    public void TryParseLimitsWorks_UnexpectedHeaderProperty_NotEnough()
    {
        var dictionary = new Dictionary<string, IEnumerable<string>>
        {
            [TooManyRequestsException.RateLimitHeader] = new[] { "user-hour-lim" },
        };

        Assert.Equal(ETooManyRequestParseLimit.UnexpectedHeaderProperty, dictionary.TryParseLimits(out _));
    }

    [Fact]
    public void TryParseLimitsWorks_UnexpectedHeaderProperty_TooMany()
    {
        var dictionary = new Dictionary<string, IEnumerable<string>>
        {
            [TooManyRequestsException.RateLimitHeader] = new[] { "user-hour-lim:35:00" },
        };

        Assert.Equal(ETooManyRequestParseLimit.UnexpectedHeaderProperty, dictionary.TryParseLimits(out _));
    }

    [Fact]
    public void TryParseLimitsWorks_HeaderPropertyValueIsNotANumber()
    {
        var dictionary = new Dictionary<string, IEnumerable<string>>
        {
            [TooManyRequestsException.RateLimitHeader] = new[] { "user-hour-lim:hour" },
        };

        Assert.Equal(ETooManyRequestParseLimit.HeaderPropertyValueIsNotANumber, dictionary.TryParseLimits(out _));
    }

    [Fact]
    public void TryParseLimitsWorks_HeaderContainsUnknownProperty()
    {
        var dictionary = new Dictionary<string, IEnumerable<string>>
        {
            [TooManyRequestsException.RateLimitHeader] = new[] { "user-hour-limit:35" },
        };

        Assert.Equal(ETooManyRequestParseLimit.HeaderContainsUnknownProperty, dictionary.TryParseLimits(out _));
    }
}
