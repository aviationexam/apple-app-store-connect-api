using Apple.AppStoreConnect.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Apple.AppStoreConnect;

public class DefaultJwtGenerator : IJwtGenerator
{
    private readonly IMemoryCache _cache;
    private readonly ISystemClock _clock;
    private readonly ILogger _logger;
    private readonly CryptoProviderFactory _cryptoProviderFactory;

    public DefaultJwtGenerator(
        IMemoryCache cache,
        ISystemClock clock,
        CryptoProviderFactory cryptoProviderFactory,
        ILogger<DefaultJwtGenerator> logger
    )
    {
        _cache = cache;
        _clock = clock;
        _cryptoProviderFactory = cryptoProviderFactory;
        _logger = logger;
    }
}
