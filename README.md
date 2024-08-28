[![Build Status](https://github.com/aviationexam/apple-app-store-connect-api/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/aviationexam/apple-app-store-connect-api/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Aviationexam.Apple.AppStoreConnect.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Aviationexam.Apple.AppStoreConnect/)
[![feedz.io](https://img.shields.io/badge/endpoint.svg?url=https%3A%2F%2Ff.feedz.io%2Faviationexam%2Fapple-app-store-connect-api%2Fshield%2FAviationexam.Apple.AppStoreConnect%2Flatest&label=Aviationexam.Apple.AppStoreConnect)](https://f.feedz.io/aviationexam/apple-app-store-connect-api/packages/Aviationexam.Apple.AppStoreConnect/latest/download)

# Apple AppStoreConnect

## Install
```xml
<ItemGroup>
    <PackageReference Include="Aviationexam.Apple.AppStoreConnect" Version="" />
</ItemGroup>
```
Note: Version is composed as `<Major>.<Minor>.<Patch>.<OpenApiMajor:00><OpenApiMinor:00><OpenApiPatch:00>[-<nightly[0-9]{4}>]`
e.g. `0.1.13.030500-nightly0008` means: source code version **0.1.13**, **8th** nightly build, AppStoreConnect version **3.5.0**

## Disclaimer

This library make some minor changes to the document, mainly to fix definition issues. You can examine the difference after restoring the project comparing `openapi.original.json` `openapi.json` located in the `src/Apple.AppStoreConnect/app-store-connect-openapi-specification`.

## How to configure library

Add library to the dependency container

```cs
using Aviationexam.Apple.AppStoreConnect.DependencyInjection;

IServiceCollection serviceCollection;

// you may need to add these dependencies
serviceCollection.AddMemoryCache();
using System;
serviceCollection.TryAddSingleton<TimeProvider>(TimeProvider.System);

// configure AppStoreConnect services
serviceCollection.AddAppleAppStoreConnect(optionsBuilder => optionsBuilder
  .Configure()
  .ValidateDataAnnotations()
);
// OR
serviceCollection.AddAppleAppStoreConnect(optionsBuilder => optionsBuilder
  .Bind(builder.Configuration.GetSection(MyConfigOptions.MyConfig))
  .ValidateDataAnnotations()
);
// OR
serviceCollection.AddAppleAppStoreConnect(
  optionsBuilder => optionsBuilder.Configure()
);
```

## How to use library

You can access all clients using the `AppStoreConnectApiClient`, e.g.:
```cs
var apiClient = serviceProvider.GetRequiredService<Apple.AppStoreConnect.Client.AppStoreConnectApiClient>();

var territoriesResponse = await apiClient.V1.Territories.GetAsync(
    requestConfiguration: x =>
    {
        x.QueryParameters.Fieldsterritories =
        [
            GetFieldsTerritoriesQueryParameterType.Currency,
        ];
        x.QueryParameters.Limit = PageSize;
    },
    cancellationToken
);

string appleAppId = "<id>";
var inAppPurchases = await apiClient.V1.Apps[appleAppId].InAppPurchasesV2.GetAsync(
    x => x.QueryParameters.Limit = PageSize,
    cancellationToken
);
```
