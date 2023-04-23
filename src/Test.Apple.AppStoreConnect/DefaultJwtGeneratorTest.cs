using Apple.AppStoreConnect;
using Apple.AppStoreConnect.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using Xunit;

namespace Test.Apple.AppStoreConnect;

public class DefaultJwtGeneratorTest
{
    [Fact]
    public async Task GenerateJwtTokenWorks()
    {
        var serviceCollection = new ServiceCollection()
            .Configure<AppleAuthenticationOptions>(x =>
            {
                x.KeyId = "KeyId";
                x.IssuerId = "IssuerId";
                x.TokenAudience = "appstoreconnect-v1";
                x.PrivateKey = static async (keyId, cancellationToken) =>
                {
                    var physicalFileProvider = new PhysicalFileProvider("");
                    var fileInfo = physicalFileProvider.GetFileInfo($"AuthKey_{keyId}.p8");
                    await using var stream = fileInfo.CreateReadStream();
                    using var reader = new StreamReader(stream);

                    return (await reader.ReadToEndAsync(cancellationToken)).AsMemory();
                };
                x.ClientSecretExpiresAfter = TimeSpan.FromMinutes(30);
            })
            .AddSingleton<IJwtGenerator, DefaultJwtGenerator>()
            .AddMemoryCache()
            .AddLogging(loggingBuilder =>
            {
                loggingBuilder
                    .AddFilter("Apple.AppStoreConnect", LogLevel.Trace)
                    .AddConsole()
                    .AddDebug();
            })
            .AddSingleton<ISystemClock, SystemClock>()
            .AddSingleton<CryptoProviderFactory>(_ => CryptoProviderFactory.Default);

        serviceCollection.TryAddEnumerable(ServiceDescriptor
            .Singleton<IPostConfigureOptions<AppleAuthenticationOptions>, AppleAuthenticationPostConfigure>()
        );
        serviceCollection.TryAddEnumerable(ServiceDescriptor
            .Singleton<IValidateOptions<AppleAuthenticationOptions>, AppleAuthenticationOptionsValidate>()
        );

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var jwtGenerator = serviceProvider.GetRequiredService<IJwtGenerator>();

        var jwtToken = await jwtGenerator.GenerateJwtTokenAsync();

        Assert.NotNull(jwtToken);
    }
}
