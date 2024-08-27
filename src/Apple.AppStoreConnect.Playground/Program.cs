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
    .ConfigureServices(services => services.AddAppleAppStoreConnect())
    .Build();

var appStoreConnectApiClient = host.Services.GetRequiredService<AppStoreConnectApiClient>();

var apps = await appStoreConnectApiClient.V1.Apps.GetAsync();

foreach (var app in apps?.Data ?? [])
{
    Console.WriteLine(app.Id);
}
