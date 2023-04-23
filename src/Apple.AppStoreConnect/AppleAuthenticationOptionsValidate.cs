using Microsoft.Extensions.Options;
using System;

namespace Apple.AppStoreConnect;

public sealed class AppleAuthenticationOptionsValidate : IValidateOptions<AppleAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, AppleAuthenticationOptions options)
    {
        if (options.ClientSecretExpiresAfter <= TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail(
                $"The '{nameof(options.ClientSecretExpiresAfter)}' option must be a positive value, '{options.ClientSecretExpiresAfter}' given."
            );
        }

        return ValidateOptionsResult.Success;
    }
}
