using Apple.AppStoreConnect.Exceptions;
using Apple.AppStoreConnect.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Apple.AppStoreConnect.Extensions;

public static class TooManyRequestsExtensions
{
    public const string UserHourLimit = "user-hour-lim";
    public const string UserHourRemaining = "user-hour-rem";

    public static ETooManyRequestParseLimit TryParseLimits(
        this TooManyRequestsException exception,
        out AppleRateLimitResponse rateLimitResponse
    ) => exception.Headers.TryParseLimits(out rateLimitResponse);

    public static ETooManyRequestParseLimit TryParseLimits(
        this IReadOnlyDictionary<string, IEnumerable<string>> headers,
        out AppleRateLimitResponse rateLimitResponse
    )
    {
        rateLimitResponse = new AppleRateLimitResponse();

        if (!headers.TryGetValue(TooManyRequestsException.RateLimitHeader, out var rateLimitEnumerable))
        {
            return ETooManyRequestParseLimit.MissingHeader;
        }

        var rateLimits = rateLimitEnumerable.ToList();

        if (rateLimits.Count > 1)
        {
            return ETooManyRequestParseLimit.TooManyHttpHeaders;
        }

        var rateLimit = rateLimits.Single();

        var rateLimitProperties = rateLimit.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var rateLimitProperty in rateLimitProperties)
        {
            var keyValue = rateLimitProperty.Split(':', StringSplitOptions.RemoveEmptyEntries);

            if (keyValue.Length != 2)
            {
                return ETooManyRequestParseLimit.UnexpectedHeaderProperty;
            }

            if (int.TryParse(keyValue[1], out var intValue))
            {
                switch (keyValue[0])
                {
                    case UserHourLimit:
                        rateLimitResponse = rateLimitResponse with { UserHourLimit = intValue };

                        break;
                    case UserHourRemaining:
                        rateLimitResponse = rateLimitResponse with { UserHourRemaining = intValue };

                        break;

                    default:
                        return ETooManyRequestParseLimit.HeaderContainsUnknownProperty;
                }
            }
            else
            {
                return ETooManyRequestParseLimit.HeaderPropertyValueIsNotANumber;
            }
        }

        return ETooManyRequestParseLimit.Success;
    }
}
