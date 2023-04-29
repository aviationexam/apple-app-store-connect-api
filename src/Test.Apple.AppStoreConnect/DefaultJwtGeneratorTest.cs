using Apple.AppStoreConnect;
using Apple.AppStoreConnect.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
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
                x.KeyId = "TestKeyId";
                x.IssuerId = "9E35F3F8-D597-45D3-84FE-5F8A386C070B";
                x.TokenAudience = "appstoreconnect-v1";
                x.PrivateKey = async (keyId, cancellationToken) =>
                {
                    var type = GetType();
                    var fileProvider = new EmbeddedFileProvider(type.Assembly, type.Namespace);
                    var fileInfo = fileProvider.GetFileInfo($"AuthKey_{keyId}.p8");
                    await using var stream = fileInfo.CreateReadStream();
                    using var reader = new StreamReader(stream);

                    return (await reader.ReadToEndAsync(cancellationToken)).AsMemory();
                };
                x.JwtExpiresAfter = TimeSpan.FromMinutes(20);
            })
            .AddSingleton<IJwtGenerator, DefaultJwtGenerator>()
            .AddMemoryCache()
            .AddLogging(loggingBuilder => loggingBuilder
                .AddFilter("Apple.AppStoreConnect", LogLevel.Trace)
                .AddConsole()
                .AddDebug()
            )
            .AddSingleton<ISystemClock, SystemClock>();

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

    [Theory]
    [InlineData(10)]
    public async Task TwoGenerateJwtTokenWorks(int delay)
    {
        var serviceCollection = new ServiceCollection()
            .Configure<AppleAuthenticationOptions>(x =>
            {
                x.KeyId = "TestKeyId";
                x.IssuerId = "9E35F3F8-D597-45D3-84FE-5F8A386C070B";
                x.TokenAudience = "appstoreconnect-v1";
                x.PrivateKey = async (keyId, cancellationToken) =>
                {
                    var type = GetType();
                    var fileProvider = new EmbeddedFileProvider(type.Assembly, type.Namespace);
                    var fileInfo = fileProvider.GetFileInfo($"AuthKey_{keyId}.p8");
                    await using var stream = fileInfo.CreateReadStream();
                    using var reader = new StreamReader(stream);

                    return (await reader.ReadToEndAsync(cancellationToken)).AsMemory();
                };
                x.JwtExpiresAfter = TimeSpan.FromSeconds(delay);
            })
            .AddSingleton<IJwtGenerator, DefaultJwtGenerator>()
            .AddMemoryCache()
            .AddLogging(loggingBuilder => loggingBuilder
                .AddFilter("Apple.AppStoreConnect", LogLevel.Trace)
                .AddConsole()
                .AddDebug()
            )
            .AddSingleton<ISystemClock, SystemClock>();

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

        await Task.Delay(TimeSpan.FromSeconds(delay + 2));

        var secondJwtToken = await jwtGenerator.GenerateJwtTokenAsync();

        Assert.NotNull(secondJwtToken);
    }
}
