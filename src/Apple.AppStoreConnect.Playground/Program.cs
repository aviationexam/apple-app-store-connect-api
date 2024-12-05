using Apple.AppStoreConnect.Client;
using Apple.AppStoreConnect.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(_ => { })
    .ConfigureAppConfiguration(x => x
        .AddJsonFile("appsettings.json5", optional: false)
        .AddJsonFile("appsettings.Debug.json5", optional: true)
    )
    .ConfigureServices(services => services
        .AddSingleton(TimeProvider.System)
        .AddMemoryCache()
        .AddAppleAppStoreConnect(x => x
            .Configure<IConfiguration>(static (x, configuration) =>
                {
                    var section = configuration.GetSection("AppStoreConnect");
                    x.KeyId = section.GetValue<string>("KeyId")!;
                    x.IssuerId = section.GetValue<string>("IssuerId")!;
                    x.SetIssuedAt = section.GetValue<bool>("SetIssuedAt");
                    x.JwtExpiresAfter = section.GetValue<TimeSpan>("JwtExpiresAfter");

                    x.PrivateKey = (string keyId, CancellationToken _) => Task.FromResult(section.GetSection("PrivateKey").GetValue<string>(keyId).AsMemory());
                }
            )
            .ValidateOnStart()
        )
    )
    .Build();

var appStoreConnectApiClient = host.Services.GetRequiredService<AppStoreConnectApiClient>();

var apps = await appStoreConnectApiClient.V1.Apps.GetAsync();

foreach (var app in apps?.Data ?? [])
{
    Console.WriteLine(app.Id);
}
