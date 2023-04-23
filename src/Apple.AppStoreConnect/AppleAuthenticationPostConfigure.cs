using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Apple.AppStoreConnect;

public sealed class AppleAuthenticationPostConfigure : IPostConfigureOptions<AppleAuthenticationOptions>
{
    public void PostConfigure(string? name, AppleAuthenticationOptions options)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (options.SecurityTokenHandler is null)
        {
            options.SecurityTokenHandler = new JsonWebTokenHandler
            {
                SetDefaultTimesOnTokenCreation = false,
            };
        }
    }
}
