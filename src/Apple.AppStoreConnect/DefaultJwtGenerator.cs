using Apple.AppStoreConnect.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Apple.AppStoreConnect;

public partial class DefaultJwtGenerator(
    IMemoryCache cache,
    TimeProvider timeProvider,
    IOptions<AppleAuthenticationOptions> authenticationOptions,
    ILogger<DefaultJwtGenerator> logger
) : IJwtGenerator
{
    /// <inheritdoc />
    public async Task<string> GenerateJwtTokenAsync(
        CancellationToken cancellationToken
    )
    {
        var appleAuthenticationOptions = authenticationOptions.Value;
        var key = CreateCacheKey(appleAuthenticationOptions);

        var clientSecret = await cache.GetOrCreateAsync(key, async entry =>
        {
            try
            {
                var (jwtToken, expiresAt) = await GenerateNewSecretAsync(
                    appleAuthenticationOptions, cancellationToken
                );
                entry.AbsoluteExpiration = expiresAt;

                return jwtToken;
            }
            catch (Exception ex)
            {
                Log.JwtTokenGenerationFailed(logger, ex, appleAuthenticationOptions.KeyId);
                throw;
            }
        });

        return clientSecret!;
    }

    private static string CreateCacheKey(
        AppleAuthenticationOptions options
    )
    {
        var segments = new[]
        {
            nameof(DefaultJwtGenerator),
            "ClientSecret",
            options.KeyId
        };

        return string.Join('+', segments);
    }

    private async Task<(string ClientSecret, DateTimeOffset ExpiresAt)> GenerateNewSecretAsync(
        AppleAuthenticationOptions appleAuthenticationOptions, CancellationToken cancellationToken
    )
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(appleAuthenticationOptions.JwtExpiresAfter);

        Log.GeneratingNewJwtToken(logger, appleAuthenticationOptions.KeyId, expiresAt);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = appleAuthenticationOptions.TokenAudience,
            IssuedAt = appleAuthenticationOptions.SetIssuedAt ? now.UtcDateTime : null,
            Expires = expiresAt.UtcDateTime,
            Issuer = appleAuthenticationOptions.IssuerId,
        };

        var pem = await appleAuthenticationOptions.PrivateKey!(
            appleAuthenticationOptions.KeyId, cancellationToken
        );

        string jwtToken;

        using (var algorithm = CreateAlgorithm(pem))
        {
            tokenDescriptor.SigningCredentials = CreateSigningCredentials(appleAuthenticationOptions.KeyId, algorithm);

            jwtToken = appleAuthenticationOptions.SecurityTokenHandler.CreateToken(tokenDescriptor);
        }

        Log.GeneratedNewJwtToken(logger, jwtToken);

        return (jwtToken, expiresAt);
    }

    private static ECDsa CreateAlgorithm(ReadOnlyMemory<char> pem)
    {
        var algorithm = ECDsa.Create();

        try
        {
            algorithm.ImportFromPem(pem.Span);
            return algorithm;
        }
        catch (Exception)
        {
            algorithm.Dispose();
            throw;
        }
    }

    private SigningCredentials CreateSigningCredentials(string keyId, ECDsa algorithm)
    {
        var key = new ECDsaSecurityKey(algorithm)
        {
            KeyId = keyId,
        };

        return new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory
            {
                CacheSignatureProviders = false,
            },
        };
    }

    private static partial class Log
    {
        [LoggerMessage(
            1, LogLevel.Error,
            "Failed to generate new JWT token for the {KeyId} KeyId."
        )]
        internal static partial void JwtTokenGenerationFailed(
            ILogger logger,
            Exception exception,
            string keyId
        );

        [LoggerMessage(
            2, LogLevel.Debug,
            "Generating new JWT token for KeyId {KeyId} that will expire at {ExpiresAt}."
        )]
        internal static partial void GeneratingNewJwtToken(
            ILogger logger,
            string keyId,
            DateTimeOffset expiresAt
        );

        [LoggerMessage(
            3, LogLevel.Trace, "Generated new JWT token with value {JwtToken}."
        )]
        internal static partial void GeneratedNewJwtToken(
            ILogger logger,
            string jwtToken
        );
    }
}
