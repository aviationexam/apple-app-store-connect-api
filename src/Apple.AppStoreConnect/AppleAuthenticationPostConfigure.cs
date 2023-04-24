using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using System;

namespace Apple.AppStoreConnect;

public sealed class AppleAuthenticationPostConfigure : IPostConfigureOptions<AppleAuthenticationOptions>
{
    public void PostConfigure(string? name, AppleAuthenticationOptions options)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (options.TokenAudience is null)
        {
            options.TokenAudience = "appstoreconnect-v1";
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (options.SecurityTokenHandler is null)
        {
            options.SecurityTokenHandler = new JsonWebTokenHandler
            {
                SetDefaultTimesOnTokenCreation = false,
            };
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (options.JwtExpiresAfter == TimeSpan.Zero)
        {
            options.JwtExpiresAfter = TimeSpan.FromMinutes(20);
        }
    }
}
