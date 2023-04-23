using Microsoft.Extensions.Options;
using System;

namespace Apple.AppStoreConnect;

public sealed class AppleAuthenticationOptionsValidate : IValidateOptions<AppleAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, AppleAuthenticationOptions options)
    {
        if (options.JwtExpiresAfter <= TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail(
                $"The '{nameof(options.JwtExpiresAfter)}' option must be a positive value, '{options.JwtExpiresAfter}' given."
            );
        }

        if (options.JwtExpiresAfter > TimeSpan.FromMinutes(20))
        {
            return ValidateOptionsResult.Fail(
                $"The '{nameof(options.JwtExpiresAfter)}' option must not be bigger than 20 minutes, '{options.JwtExpiresAfter}' given."
            );
        }

        return ValidateOptionsResult.Success;
    }
}
